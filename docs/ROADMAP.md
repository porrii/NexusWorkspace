# NexusWorkspace — Roadmap de implementación

Incremental. **La solución permanece compilable y ejecutable al terminar cada fase.**

| Fase | Contenido | Estado al terminar |
|---|---|---|
| **0 · Cimientos** | Solución (6 proyectos + Tests), host de DI, Serilog, EF Core + SQLite, 1ª migración, `IAppPaths`, `ISettingsStore`, shell con sidebar y cambio de tema, CI de build. | `NexusWorkspace.Desktop` arranca, tema claro/oscuro, `nexus.db` creado. |
| **1 · Núcleo** | Proyectos + Tareas + Subtareas + Checklist + Comentarios + **historial inmutable** + **barra de acciones rápidas**. Soft-delete + archivado. Datos demo. Tests de transiciones e historial. | MVP del bucle diario: crear proyecto → tarea → acción rápida → historial. |
| **2 · Dashboard + captura** | Widgets del Dashboard con layout persistido · Captura rápida flotante + **hotkey global** + parser · Inbox + "convertir a…" · Command Palette (Ctrl+K) · Búsqueda global FTS5 (Ctrl+F). | Se abre por la mañana y responde "¿qué tengo hoy?" en un vistazo. |
| **3 · Seguimientos** | Entidad `FollowUp` + widget "esperando respuesta" con contador de días + "enviar recordatorio" · Recordatorios + scheduler + notificaciones Windows · Calendario mes/semana/día. | Seguimiento de terceros y avisos locales sin internet. |
| **4 · Personas y contexto** | Personas y Empresas con su histórico agregado · Comunicaciones y Reuniones · Etiquetas + filtros · Relaciones y referencias cruzadas + deep links · Búsquedas guardadas y favoritos. | "Todo lo relacionado con Igor / con Axon" en una pantalla. |
| **5 · Vistas y archivos** | Kanban con arrastrar (→ estado + historial) · Timeline de proyecto · Adjuntos: arrastrar-soltar, pegar del portapapeles, miniaturas, almacén externo · Sección Archivos. | Gestión visual y documental completa. |
| **6 · Analítica** | Estadísticas con LiveCharts2 · Informes PDF/Excel/CSV/JSON · Plantillas (árboles de proyecto/tarea) · Importación JSON/CSV + **importador del bloc de notas**. | Informes profesionales y migración de la información antigua. |
| **7 · Robustez** | Backups automáticos/programados + antes de migrar + restaurar · Exportar/Importar Workspace · Configuración completa · Onboarding + workspace demo · Accesibilidad, rendimiento (100k tareas), manejo de errores. | Producto fiable para uso diario durante años. |
| **8 · Android** | Head Android, shell táctil (drawer + bottom nav), layouts adaptados, servicios de plataforma. | Misma información en Windows y Android. |
| **Post-MVP** | Instalador Windows (Inno Setup / Velopack, preserva datos) · activar stubs: Sync, IA local, OCR, integraciones. | La arquitectura ya lo permite. |

## Progreso

- [x] **Fase 0** — andamiaje de la solución (6 proyectos + tests), DI, Serilog, EF Core + SQLite, `AppPaths`, `SettingsStore`, shell con barra lateral + temas
- [x] **Fase 1** — Proyectos · Tareas · Subtareas · Checklist · Comentarios · **historial inmutable** · **barra de acciones rápidas** · soft-delete + archivado · datos demo · tests
- [x] **Fase 2** — Dashboard configurable (widgets on/off persistidos) + captura rápida inline · **ventana flotante de Captura rápida** + **hotkey global** Ctrl+Shift+Espacio (Windows) + parser local de reglas · **Inbox** + convertir a Tarea/Proyecto · **Command Palette** (Ctrl+K) · **búsqueda global FTS5** (Ctrl+F, sin acentos, prefijo) · tests del parser, Inbox y búsqueda

- [x] **Fase 3** — **Seguimientos** (`FollowUp`): esperando respuesta de persona/empresa/etiqueta, contador de días, "enviar recordatorio" (nº + historial), escalar, marcar respondido/cerrado; página propia + widget en Dashboard + sección en Tarea/Proyecto. **Recordatorios** (`Reminder`) + **scheduler en proceso** (cada 45 s) que lanza notificaciones al vencer. **Centro de notificaciones** local (campana + badge) + **toasts** in-app. **Calendario** mensual que agrega vencimientos de tareas/proyectos, próximos seguimientos y recordatorios.

> **Estado 2026-09-03**: Fases 0–2 validadas en el PC de compilación (`dotnet build` 0/0,
> migración EF `Initial`, **39/39 tests**). Fase 3 código completo, pendiente de compilar
> — añade tablas nuevas, ejecutar **`build.bat migrate`**.

> Publicado en GitHub: `porrii/NexusWorkspace`, rama `dev`. `main` + release cuando la v1 esté lista.
> CI: el flujo está en `docs/ci-build.yml`; moverlo a `.github/workflows/build.yml` desde la web de
> GitHub (o tras `gh auth refresh -s workflow`) para activarlo.
