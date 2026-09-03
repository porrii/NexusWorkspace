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

- [ ] **Fase 0** — andamiaje de la solución, DI, EF Core, shell + temas
- [ ] **Fase 1** — Proyectos · Tareas · Historial
- [ ] Fase 2 …
