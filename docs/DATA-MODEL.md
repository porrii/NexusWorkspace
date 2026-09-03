# NexusWorkspace — Modelo de datos

Todas las entidades relevantes heredan de `AuditableEntity` e implementan
`ISoftDelete` + `IArchivable`:

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | GUID v7 (`Guid.CreateVersion7()`), ordenable en el tiempo. |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTime` (UTC) | Los fija el interceptor de auditoría. |
| `IsArchived` / `ArchivedAtUtc` | `bool` / `DateTime?` | Fuera de vistas activas, sigue en Archivados. |
| `IsDeleted` / `DeletedAtUtc` | `bool` / `DateTime?` | Papelera lógica (soft-delete), *global query filter*. |

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
| `Person` | `Name, Role, CompanyId, Email, Phone, Notes` | ∞—∞ `Project`/`WorkTask` · 1—∞ `Communication`/`Meeting`/`FollowUp` |
| `Company` | `Name, Kind (cliente/proveedor/interno), Notes` | 1—∞ `Person` · ∞—∞ `Project` · 1—∞ `Communication` |
| `Tag` | `Name, Color` | ∞—∞ `Project`/`WorkTask` (`ProjectTag`, `WorkTaskTag`) |
| `Comment` | `TargetKind, TargetId, Body` | polimórfico |
| `Attachment` | `TargetKind, TargetId, FileName, RelativePath, ContentHash, SizeBytes, ThumbnailPath, MimeType` | polimórfico · fichero en disco |
| `ActivityEvent` | `TargetKind, TargetId, ProjectId, Type, ActorLabel, OccurredAtUtc, OldValue, NewValue, Note` | **append-only**, toca todo, nunca se borra |
| `FollowUp` | `TargetKind, TargetId, ProjectId, WaitingOnPersonId, WaitingOnCompanyId, WaitingSinceUtc, LastContactUtc, NextFollowUpUtc, ReminderCount, State` | "esperando respuesta de" · días en espera calculados |
| `Communication` | `TargetKind, TargetId, ProjectId, Kind, Direction, PersonId, CompanyId, OccurredAtUtc, Summary, Body` | polimórfico + `Person`/`Company` |
| `Meeting` | `ProjectId, Title, StartUtc, EndUtc, Location, Notes` | ∞—∞ `Person` (asistentes) |
| `Reminder` | `TargetKind, TargetId, Text, RemindAtUtc, Status` | a cualquier entidad |
| `InboxItem` | `RawText, ParsedHintJson, ConvertedToKind, ConvertedToId` | se convierte en `WorkTask`/`Project`/nota/incidencia/`FollowUp` |
| `SavedSearch` | `Name, QueryJson, IsFavorite` | filtros guardados |
| `Template` | `Name, Kind, DefinitionJson` | genera árboles de proyecto/tarea |
| `Notification` | `Text, Kind, CreatedAtUtc, IsRead, DeepLink` | centro de notificaciones local |
| `Setting` | `Key, Value` | solo ajustes de negocio (UI/app → `settings.json`) |

## Enums

- **`ProjectStatus`**: `Planning, Active, OnHold, Blocked, Finished` (archivado = flag ortogonal).
- **`WorkTaskStatus`**: `Pending, InProgress, WaitingClient, WaitingProvider, Blocked, Finished, Cancelled` (archivada = flag ortogonal).
- **`Priority`**: `Critical, High, Medium, Low`.
- **`ActivityType`**: `Created, Updated, StatusChanged, PriorityChanged, DueDateChanged, AssigneeChanged, CommentAdded, AttachmentAdded, AttachmentRemoved, SubTaskAdded, SubTaskCompleted, ChecklistItemToggled, DependencyAdded, DependencyRemoved, FollowUpStarted, FollowUpReminderSent, FollowUpResolved, QuickAction, RelationAdded, RelationRemoved, MovedProject, Archived, Restored, Trashed, RestoredFromTrash, Deleted, Duplicated, TemplateApplied, Imported`.
- **`QuickActionKind`**: `EmailSent, EmailReceived, CallMade, MeetingHeld, InfoSent, InfoReceived, PendingClient, PendingProvider, IncidentDetected, IncidentResolved, DeployedDev, DeployedPre, DeployedPro, ReminderSent, ChangeRequested, TestPerformed`.
- **`EntityKind`**: `Project, WorkTask, SubTask, ChecklistItem, Person, Company, Tag, Comment, Attachment, FollowUp, Communication, Meeting, Reminder, InboxItem`.
- **`CompanyKind`**: `Client, Provider, Internal, Other`.
- **`CommunicationKind`**: `Email, Call, Meeting, Message, InfoExchange, Other`.
- **`CommunicationDirection`**: `Outgoing, Incoming, Internal`.
- **`DependencyKind`**: `FinishToStart, Blocks, Related`.
- **`FollowUpState`**: `Waiting, Answered, Escalated, Closed`.
- **`ReminderStatus`**: `Pending, Done, Dismissed`.

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
