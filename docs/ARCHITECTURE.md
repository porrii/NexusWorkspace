# NexusWorkspace — Arquitectura

Fuente viva. El documento visual equivalente (con diagramas y wireframes) está en
<https://claude.ai/code/artifact/b9e28e8b-0851-4844-916b-385f9f3b03a9>.

---

## 1. Principios rectores (en orden de prioridad)

1. **Fiabilidad** — ninguna operación destructiva silenciosa.
2. **No perder información** — historial *append-only* + *soft-delete* + archivado.
3. **Facilidad de uso** — toda acción en un clic o por teclado.
4. **Rendimiento** — 100 000+ tareas, 50 000+ eventos, 10 000+ proyectos.
5. **Diseño visual** — identidad propia, claro / oscuro / sistema.
6. **Escalabilidad y mantenibilidad** — Clean Architecture, SOLID, DI, sin sobreingeniería.

---

## 2. Capas y regla de dependencia

```
Desktop (head Win)  Android (head)          ← solo componen la DI en el arranque
        └──────────────┬─────────┘
                 NexusWorkspace.UI           ← Avalonia XAML + ViewModels (compartido)
                        │  usa
                 NexusWorkspace.Application   ← casos de uso + puertos (interfaces)
                        │  usa
                 NexusWorkspace.Domain        ← entidades, reglas, enums — 0 dependencias
                        ▲  implementa puertos
                 NexusWorkspace.Infrastructure ← EF Core/SQLite, archivos, backups, plataforma
```

- **La flecha de dependencia apunta siempre hacia `Domain`.**
- `Infrastructure` depende de `Application` (implementa sus interfaces), nunca al revés.
- Los *heads* son el único punto que conoce `Infrastructure`, y solo para registrarla en el contenedor de DI.

### Proyectos

| Proyecto | Responsabilidad |
|---|---|
| `NexusWorkspace.Domain` | Entidades, value objects, enums, reglas puras, máquinas de estado, excepciones de dominio. |
| `NexusWorkspace.Application` | Servicios de aplicación / casos de uso, puertos (`I…Service`, `I…Store`), DTOs, validadores, política de archivado/borrado, parser de captura rápida. |
| `NexusWorkspace.Infrastructure` | `NexusDbContext` (EF Core + SQLite), configuraciones, migraciones, interceptores (auditoría, `ActivityEvent`, soft-delete, FTS), almacén de archivos, backup, exportación/importación, scheduler, Serilog. |
| `NexusWorkspace.UI` | Vistas (XAML), ViewModels, sistema de diseño, controles, navegación/diálogos, shell adaptable. Compartido Windows + Android. |
| `NexusWorkspace.Desktop` | Head Windows: `Program.cs`, composición DI, hotkey global, toasts, file pickers, rutas, ventana principal + flotante de captura. |
| `NexusWorkspace.Android` | Head Android (Fase 8). |
| `NexusWorkspace.Tests` | xUnit: unit (dominio, servicios, parser) + integración SQLite. |

### Patrones

- **MVVM** con CommunityToolkit.Mvvm (source generators). Sin lógica de negocio en las Views.
- **Repository + Unit of Work** finos sobre `DbContext`; lecturas con proyección.
- **`Result<T>`** para errores esperables; excepciones solo para lo inesperado.
- **Mensajería in-app** (`WeakReferenceMessenger`) para refrescos entre pantallas.
- **Interceptores EF** para auditoría, `UpdatedAtUtc`, soft-delete y sincronización de FTS.

### Lo que NO se hace (anti-sobreingeniería)

Sin microservicios, sin servidor local, sin API interna, sin contenedores, sin event
sourcing completo, sin CQRS con buses, sin dependencias cloud obligatorias, sin login,
sin framework de plugins (solo interfaces que lo permitan luego).

---

## 3. Almacenamiento y datos

| Elemento | Dónde |
|---|---|
| Base de datos | `nexus.db` único (SQLite, modo WAL), EF Core 9. |
| Archivos adjuntos | En disco: `files/{kind}/{id}/…`, con hash de contenido (deduplicado) y miniaturas. La BD guarda solo metadatos + ruta relativa. |
| Configuración | `settings.json` (System.Text.Json): tema, ventana, layout de widgets, atajos. Nunca mezclada con datos de negocio. |
| Logs | `logs/nexus-YYYYMMDD.log` (Serilog, rolling). |
| Backups | `backups/nexus_YYYY-MM-DD_HHmmss.db` — nunca se sobrescriben. |

### Ubicación (Windows)

```
%APPDATA%\NexusWorkspace\
├─ nexus.db  nexus.db-wal  nexus.db-shm
├─ settings.json
├─ files\      backups\      logs\      exports\
```

Modo portable: si existe `nexus.portable` junto al ejecutable, todo va a `.\data\`.

### Reglas de datos

- Solo **EF Core Migrations**. Nunca se modifica la BD a mano.
- Al arrancar: migraciones pendientes → **backup automático** → aplicar → si falla, restaurar y avisar.
- Toda entidad importante: `Id` (**GUID v7**, `Guid.CreateVersion7()`), `CreatedAtUtc`, `UpdatedAtUtc`, `IsArchived` + `ArchivedAtUtc`, `IsDeleted` + `DeletedAtUtc`. Las consultas normales filtran `IsDeleted` con *global query filter*.
- **Fechas siempre en UTC**; la UI muestra hora local.
- Cifrado: abstracciones `IDbProtector` / `IFileProtector` preparadas (SQLCipher o cifrado de fichero) sin afectar al resto.

---

## 4. Historial inmutable (memoria permanente)

- Tabla `ActivityEvent` **append-only**: `TargetKind` + `TargetId` (referencia polimórfica), `ProjectId` denormalizado, `Type` (enum), `ActorLabel`, `OccurredAtUtc`, `OldValue`, `NewValue`, `Note`.
- Se genera **automáticamente** por interceptores EF en cada cambio relevante y por las acciones rápidas. El usuario nunca la edita ni introduce fechas a mano.
- Sobrevive al archivado, a la papelera y al borrado físico de la entidad de origen.
- No es event sourcing: las entidades son filas mutables normales; el `ActivityEvent` es un registro paralelo.

### Ciclo de vida de una entidad

| Acción | Efecto |
|---|---|
| Archivar | `IsArchived = true` → sale de las vistas activas, sigue en Archivados / búsqueda / estadísticas. |
| Papelera | `IsDeleted = true` + `DeletedAtUtc` → oculto, restaurable. |
| Borrado físico | Acción explícita + **doble confirmación** + **backup automático** → la fila desaparece; el `ActivityEvent` permanece. |

---

## 5. Búsqueda

- **SQLite FTS5**: tabla virtual con un "documento de búsqueda" por entidad (título + cuerpo + etiquetas + proyecto + personas + comentarios).
- Sincronizada por interceptores EF / triggers.
- Búsqueda incremental (mientras se escribe), offline, con contexto y resaltado.

---

## 6. Backups

- Antes de cada migración; y de forma programada (diario / semanal / manual).
- `checkpoint` de WAL → copia de `nexus.db` → `backups/nexus_<timestamp>.db`. Nunca sobrescribe.
- Rotación configurable (conservar N). Restauración con confirmación, creando antes un backup del estado actual.
- **Exportar Workspace**: paquete `.nexuszip` = BD + `files/` + `settings.json` + `manifest.json`. Permite trasladar todo a otro equipo.

---

## 7. Notificaciones y recordatorios

- 100 % locales. `ISchedulerService` (temporizador en proceso, agenda persistida) dispara `INotificationService`.
- Windows: toasts nativos. Android (Fase 8): notificaciones del sistema.
- Sin internet, sin cuenta. Opcionales y configurables.

---

## 8. Estrategia multiplataforma

- **~92 % del código compartido**: Domain + Application + UI (vistas Avalonia + ViewModels).
- **~8 % por plataforma**: atajo global, notificaciones, file pickers, rutas de almacenamiento, ciclo de vida, shell adaptable (sidebar ↔ navigation drawer), objetivos táctiles / gestos.
- Toda E/S de plataforma detrás de interfaces (`IAppPaths`, `INotificationService`, `IGlobalHotkeyService`, `IFilePickerService`). Añadir Android = implementar ~8 servicios + layouts táctiles.
- Android **no** es "el Windows comprimido": layouts responsivos, `OnPlatform`, controles adaptables.

---

## 9. Futuro (preparado, no implementado)

- **Sincronización**: `ISyncService`, `IRemoteStorage`, `ICloudProvider`, `IConflictResolver` — solo interfaces + *no-op*. La base (GUID v7, `UpdatedAtUtc`, soft-delete) ya lo permite sin rediseñar el esquema.
- **IA**: `IAiAssistant`, `IOcrService` — interfaces. Modelos locales u opt-in cloud, siempre indicando si los datos salen del dispositivo.
- **Integraciones** (Outlook, GitHub, Teams, Jira), plugins, automatizaciones: contempladas en la arquitectura, fuera del alcance actual.

---

## 10. Desviaciones respecto al brief original (justificadas)

| Área | Cambio | Motivo |
|---|---|---|
| Nº de proyectos | 9 → **6** (+ Tests) | Core/Shared eran "cajón de sastre". Se conservan las fronteras que importan con menos fricción de build. |
| UI multiplataforma | Dos clientes → **un proyecto UI compartido** + heads finos | Avalonia 11 renderiza el mismo XAML en Windows y Android. |
| Historial | Event sourcing → **tabla `ActivityEvent` append-only** junto a entidades mutables | Da la memoria permanente sin el coste de reconstruir estado. |
| Fechas | "Fecha del sistema" → **UTC almacenado, local mostrado** | Un histórico permanente debe seguir siendo correcto tras cambios de zona/horario. |
| IDs | "Identificador interno" → **GUID v7** como PK | Ordenable en el tiempo y apto para fusión multi-dispositivo futura. |
| Búsqueda | Genérica → **SQLite FTS5** | Incremental, offline, escalable, sin motor externo. |
| Estado "Archivado" | Enum de estado → **flag `IArchivable` ortogonal** | Evita mezclar ciclo de vida con estado de negocio. |
| PDF/Excel | Sin especificar → **QuestPDF + ClosedXML** | 100 % .NET gestionado, multiplataforma, sin dependencias nativas. |
