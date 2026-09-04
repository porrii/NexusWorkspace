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
| `Project` | `Name, Description, Icon, Color, Status, Priority, StartDateUtc, DueDateUtc, CompletedDateUtc, OwnerPersonId` | 1—∞ `WorkTask` · ∞—∞ `Person`/`Company`/`Tag` · 1—∞ `FollowUp`/`Communication`/`Meeting`/`Attachment`/`Comment` |
| `WorkTask` | `Title, Description, ProjectId, Status, Priority, DueDateUtc, CompletedDateUtc, AssigneePersonId, RelatedCompanyId, SortKey` | ∞—1 `Project` · 1—∞ `SubTask`/`ChecklistItem`/`Comment`/`Attachment` · ∞—∞ `Tag`/`Person` · `TaskDependency` |
| `SubTask` | `WorkTaskId, ParentSubTaskId (recursivo), Title, IsDone, SortKey` | árbol ilimitado; % completado calculado |
| `ChecklistItem` | `WorkTaskId, Text, IsChecked, SortKey` | ∞—1 `WorkTask` |
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
| `Template` | `Name, Kind, DefinitionJson` | genera árboles de proyecto/tarea |
| `Notification` | `Text, Kind, CreatedAtUtc, IsRead, DeepLink` | centro de notificaciones local |
| `Setting` | `Key, Value` | solo ajustes de negocio (UI/app → `settings.json`) |

## Enums

- **`ProjectStatus`**: `Planning, Active, OnHold, Blocked, Finished` (archivado = flag ortogonal).
- **`WorkTaskStatus`**: `Pending, InProgress, WaitingClient, WaitingProvider, Blocked, Finished, Cancelled` (archivada = flag ortogonal).
- **`Priority`**: `Critical, High, Medium, Low`.
- **`ActivityType`**: `… RelationAdded, RelationRemoved, MovedProject, Archived, Restored, Trashed, RestoredFromTrash, Deleted, Duplicated, TemplateApplied, Imported, Renamed, CommunicationLogged, MeetingScheduled, MeetingUpdated, TagAdded, TagRemoved, LinkedPerson, UnlinkedPerson, LinkedCompany, UnlinkedCompany` (append-only; los valores numéricos existentes nunca cambian).
- **`QuickActionKind`**: `EmailSent, EmailReceived, CallMade, MeetingHeld, InfoSent, InfoReceived, PendingClient, PendingProvider, IncidentDetected, IncidentResolved, DeployedDev, DeployedPre, DeployedPro, ReminderSent, ChangeRequested, TestPerformed`.
- **`EntityKind`**: `Project, WorkTask, SubTask, ChecklistItem, Person, Company, Tag, Comment, Attachment, FollowUp, Communication, Meeting, Reminder, InboxItem, SavedSearch, Relation`.
- **`CompanyKind`**: `Other, Client, Provider, Internal`.
- **`CommunicationChannel`**: `Email, Call, Chat, InPerson, Letter, Ticket, Other`.
- **`CommunicationDirection`**: `Outbound, Inbound, Internal`.
- **`MeetingStatus`**: `Scheduled, Held, Cancelled`.
- **`RelationKind`**: `RelatesTo, Blocks, DependsOn, Duplicates, References, PartOf, Mentions`.
- **`SavedSearchKind`**: `Global, Projects, Tasks, People, Companies, FollowUps, Communications`.
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
(`StatusChanged`, con `OldValue`/`NewValue`). Las acciones rápidas pueden empujar el
estado (p. ej. `PendingProvider` → `WaitingProvider`).
