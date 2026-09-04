using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Domain.Reminders;

/// <summary>
/// A manual, local reminder ("Recordar a Igor el lunes"). Fires while the app is
/// running via the in-process scheduler; also shown on the Dashboard and Calendar.
/// May optionally be linked to any entity.
/// </summary>
public class Reminder : AuditableEntity
{
    public required string Text { get; set; }

    public DateTime RemindAtUtc { get; set; }

    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>True once the scheduler has raised a notification for it.</summary>
    public bool Notified { get; set; }

    public EntityKind? TargetKind { get; set; }

    public Guid? TargetId { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public bool IsPending => Status == ReminderStatus.Pending;

    public bool IsDue(DateTime nowUtc) => Status == ReminderStatus.Pending && RemindAtUtc <= nowUtc;
}
