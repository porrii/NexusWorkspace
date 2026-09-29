# NexusWorkspace — Modelo de datos

Todas las entidades relevantes heredan de `AuditableEntity` e implementan
`ISoftDelete` + `IArchivable`:

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | GUID v7 (`Guid.CreateVersion7()`), ordenable en el tiempo. |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTime` (UTC) | Los fija el interceptor de auditoría. |
| `IsArchived` / `ArchivedAtUtc` | `bool` / `DateTime?` | Fuera de vistas activas, sigue en Archivados. |
| `IsDeleted` / `DeletedAtUtc` | `bool` / `DateTime?` | Papelera lógica (soft-delete), *global query filter*. |

## Almacenamiento

- **GUID como texto**: todas las claves (PK/FK) se guardan como texto de 36 caracteres
  en minúsculas (`GuidToStringConverter`). Legible en la BD, consistente con el SQL crudo
  del índice de búsqueda y aún ordenable en el tiempo (v7).
- **Enums como texto** (`HaveConversion<string>()`).
- **Adjuntos fuera de la BD**: solo metadatos en `Attachments`; los bytes viven en
  `files/<xx>/<sha256><ext>` vía `IAttachmentStore`. Direccionado por contenido y
  deduplicado: dos subidas idénticas comparten un único blob físico, que solo se
  borra cuando ningún adjunto vivo lo referencia.
- **Copias de seguridad fuera de la BD**: `.zip` bajo `backups/` (BD + `settings.json`
  + `manifest.json`, opcionalmente `files/`). Restaurar / importar workspace se
  escriben en el marcador `.nexus-restore` y se aplican al arrancar, antes de abrir EF.

## Búsqueda (FTS5)

Tabla virtual `SearchIndex` (`fts5`, tokenizer `unicode61 remove_diacritics 2`), **fuera del
modelo EF** — la crea `DatabaseInitializer` / `Fts5SearchService` con SQL. Columnas:
`entity_kind, entity_id, navigate_kind, navigate_id, project_id` (UNINDEXED) + `title, body`.
Se indexan `Project`, `WorkTask` y `Comment`; un acierto en un comentario navega a su tarea.
La mantiene sincronizada `SearchIndexInterceptor` en cada `SaveChanges` (best-effort: un fallo
del índice nunca rompe un guardado; se puede reconstruir desde Configuración).

## Referencias polimórficas

`Comment`, `Attachment`, `ActivityEvent`, `FollowUp`, `Communication`, `Reminder`
apuntan a cualquier entidad con **`TargetKind` (`EntityKind`) + `TargetId` (`Guid`)**.
`ActivityEvent` y `FollowUp` además denormalizan `ProjectId` para consultas rápidas.

## Entidades

| Entidad | Campos clave | Relaciones |
|---|---|---|
| `Project` | `Name, Description, Icon, Color, Status, Priority, StartDateUtc, DueDateUtc, CompletedDateUtc, OwnerPersonId` | 1—∞ `WorkTask` · ∞—∞ `Person`/`Company`/`Tag` (`ProjectPerson` = equipo con `Role` libre, `ProjectCompany`, `ProjectTag`) · 1—∞ `FollowUp`/`Communication`/`Meeting`/`Attachment`/`Comment` |
| `WorkTask` | `Title, Description, ProjectId, Status, Priority, DueDateUtc, CompletedDateUtc, AssigneePersonId, RelatedCompanyId, SortKey` | ∞—1 `Project` · 1—∞ `SubTask`/`ChecklistItem`/`Comment`/`Attachment` · ∞—∞ `Tag`/`Person` (`WorkTaskTag`, `WorkTaskPerson` = colaboradores además del `AssigneePersonId` único) · `TaskDependency` |
| `SubTask` | `WorkTaskId, ParentSubTaskId (recursivo), Title, IsDone, SortKey` | árbol ilimitado; % completado calculado |
| `ChecklistItem` | `WorkTaskId, Text, IsChecked, SortKey` | ∞—1 `WorkTask`. **Transitorio** (docs/UX-REVIEW.md, bloques B1/B2): subtareas y checklist se ven y editan como una sola lista; nada nuevo crea `ChecklistItem` (`WorkTaskService.AddSubTaskAsync` para todo lo nuevo) y al abrir una tarea sus ítems vivos se mueven a `SubTask` (`EnsureChecklistConvertedAsync`). La entidad y la tabla siguen existiendo hasta una migración posterior a la v1 que las retire. |
| `TaskDependency` | `WorkTaskId, DependsOnWorkTaskId, Kind` | `WorkTask` ↔ `WorkTask` |
| `Person` | `Name, Role, CompanyId, Email, Phone, Notes, IsFavorite, LastContactedUtc` | ∞—1 `Company` · ∞—∞ `Project`/`WorkTask`/`Tag` (`ProjectPerson`, `WorkTaskPerson`, `PersonTag`) · 1—∞ `Communication` · ∞—∞ `Meeting` (`MeetingParticipant`) · detalle **agrega** todo lo vinculado |
| `Company` | `Name, Kind (cliente/proveedor/interno/otro), Website, Notes, IsFavorite, LastContactedUtc` | 1—∞ `Person` · ∞—∞ `Project`/`Tag` (`ProjectCompany`, `CompanyTag`) · 1—∞ `Communication` |
| `Tag` | `Name, Color, Description, IsPinned` | ∞—∞ `Project`/`WorkTask`/`Person`/`Company` (`ProjectTag`, `WorkTaskTag`, `PersonTag`, `CompanyTag`) · color de reserva determinista por nombre |
| `Comment` | `TargetKind, TargetId, Body` | polimórfico |
| `Attachment` | `TargetKind, TargetId, FileName, RelativePath, ContentHash, SizeBytes, ThumbnailPath, MimeType` | polimórfico · fichero en disco |
| `ActivityEvent` | `TargetKind, TargetId, ProjectId, Type, ActorLabel, OccurredAtUtc, OldValue, NewValue, Note` | **append-only**, toca todo, nunca se borra |
| `FollowUp` | `TargetKind, TargetId, ProjectId, WaitingOnPersonId, WaitingOnCompanyId, WaitingSinceUtc, LastContactUtc, NextFollowUpUtc, ReminderCount, State` | "esperando respuesta de" · días en espera calculados |
| `Communication` | `Channel, Direction, Subject, Body, OccurredAtUtc, PersonId, CompanyId, ProjectId, WorkTaskId, ContactLabel` | enlaces directos (FK `SetNull`) a persona/empresa/proyecto/tarea; registrar una alimenta el histórico de cada extremo y su `LastContactedUtc` |
| `Meeting` | `Title, Agenda, Notes, StartUtc, EndUtc, Location, Status (Scheduled/Held/Cancelled), ProjectId` | 1—∞ `MeetingParticipant` · aparece en el Calendario |
| `MeetingParticipant` | `MeetingId, PersonId?, ExternalName?, Role, Attended` | persona guardada **o** nombre externo libre |
| `EntityRelation` | `FromKind, FromId, ToKind, ToId, Kind, Note` | cross-reference tipada entre cualquier par de entidades; visible desde ambos extremos con deep-link; soft-delete |
| `Reminder` | `TargetKind, TargetId, Text, RemindAtUtc, Status` | a cualquier entidad |
| `InboxItem` | `RawText, ParsedHint, State (Pending/Converted/Dismissed), ConvertedToKind, ConvertedToId, ProcessedAtUtc` | captura rápida; se convierte en `WorkTask`/`Project` (descartar = soft-delete) |
| `SavedSearch` | `Name, Kind (superficie), QueryText, FiltersJson, IsPinned, SortKey, LastRunUtc` | búsquedas/filtros guardados por superficie; los fijados se muestran como chips |
| `Template` | `Name, Kind (Project/Task), Description, DefinitionJson (opaco), UseCount, LastUsedAtUtc` | árbol reutilizable: proyecto (tareas → subtareas + etiquetas) o tarea suelta; aplicarla los recrea y suma a `UseCount`. El JSON conserva un campo `Checklist` por compatibilidad con plantillas antiguas — al aplicarse se recrea como `SubTask`, igual que todo lo demás. |
| `Notification` | `Text, Kind, CreatedAtUtc, IsRead, DeepLink` | centro de notificaciones local |
| `Setting` | `Key, Value` | solo ajustes de negocio (UI/app → `settings.json`) |

> `settings.json` (`AppSettings`, fuera de la BD) también guarda `TaskEventLabels`: la lista
> de etiquetas de "Registrar evento" en el detalle de tarea, que crece cuando el usuario
> escribe una nueva (ver `ActivityType.QuickAction` más abajo).

## Enums

- **`ProjectStatus`**: `Planning, Active, OnHold, Blocked, Finished` (archivado = flag ortogonal).
- **`WorkTaskStatus`**: `Pending, InProgress, WaitingClient, WaitingProvider, Blocked, Finished, Cancelled` (archivada = flag ortogonal).
- **`Priority`**: `Critical, High, Medium, Low`.
- **`ActivityType`**: `… RelationAdded, RelationRemoved, MovedProject, Archived, Restored, Trashed, RestoredFromTrash, Deleted, Duplicated, TemplateApplied, Imported, Renamed, CommunicationLogged, MeetingScheduled, MeetingUpdated, TagAdded, TagRemoved, LinkedPerson, UnlinkedPerson, LinkedCompany, UnlinkedCompany` (append-only; los valores numéricos existentes nunca cambian).
- **`QuickActionKind`**: `EmailSent, EmailReceived, CallMade, MeetingHeld, InfoSent, InfoReceived, PendingClient, PendingProvider, IncidentDetected, IncidentResolved, DeployedDev, DeployedPre, DeployedPro, ReminderSent, ChangeRequested, TestPerformed`. El catálogo fijo (`QuickActionCatalog`) y `WorkTaskService.ExecuteQuickActionAsync` siguen en el código pero **la UI ya no los usa** (bloque E): "Email/Llamada/Reunión" ahora son `Communication` reales (`CommunicationComposerViewModel` en la tarea) y "Pendiente cliente/proveedor" es directamente el cambio de estado. Lo que queda de la barra de un clic es **"Registrar evento"**: texto libre → `WorkTaskService.LogEventAsync` (un `ActivityEvent` de tipo `QuickAction`, sin `QuickActionKind`), con las etiquetas usadas guardadas en `AppSettings.TaskEventLabels`.
- **`EntityKind`**: `Project, WorkTask, SubTask, ChecklistItem, Person, Company, Tag, Comment, Attachment, FollowUp, Communication, Meeting, Reminder, InboxItem, SavedSearch, Relation`.
- **`CompanyKind`**: `Other, Client, Provider, Internal`.
- **`CommunicationChannel`**: `Email, Call, Chat, InPerson, Letter, Ticket, Other`.
- **`CommunicationDirection`**: `Outbound, Inbound, Internal`.
- **`MeetingStatus`**: `Scheduled, Held, Cancelled`.
- **`RelationKind`**: `RelatesTo, Blocks, DependsOn, Duplicates, References, PartOf, Mentions`.
- **`SavedSearchKind`**: `Global, Projects, Tasks, People, Companies, FollowUps, Communications`.
- **`TemplateKind`**: `Project, Task`.
- **`DependencyKind`**: `FinishToStart, Blocks, Related`.
- **`FollowUpState`**: `Waiting, Escalated, Answered, Closed`.
- **`ReminderStatus`**: `Pending, Done, Dismissed`.
- **`NotificationKind`**: `System, Reminder, FollowUp, DueDate, Meeting`.

## Transiciones de estado (`WorkTaskStatus`)

```
Pending        → InProgress, WaitingClient, WaitingProvider, Blocked, Cancelled
InProgress     → WaitingClient, WaitingProvider, Blocked, Finished, Cancelled, Pending
WaitingClient  → InProgress, Blocked, Finished, Cancelled
WaitingProvider→ InProgress, Blocked, Finished, Cancelled
Blocked        → InProgress, WaitingClient, WaitingProvider, Cancelled
Finished       → InProgress            (reabrir)
Cancelled      → Pending               (reactivar)
```

Cada transición la valida `WorkTaskStateMachine` y genera un `ActivityEvent`
(`StatusChanged`, con `OldValue`/`NewValue`).

### `FollowUp`: oculto de la interfaz (post-v1)

La entidad `FollowUp`, su servicio y el acoplo automático con
`WorkTaskService.ChangeStatusAsync` (cerrar seguimientos abiertos al salir de un estado
de espera) descritos en versiones anteriores de este documento **ya no están activos**.
Se quitó la superficie de usuario (páginas, pestañas, widget del Dashboard, atajo de
paleta de comandos) porque no aportaba valor tal y como estaba planteada, pero la
entidad, la tabla y `FollowUpService`/`FollowUpReadService` se mantienen intactos en el
código para poder rediseñarla con calma más adelante en vez de reconstruirla desde cero.
