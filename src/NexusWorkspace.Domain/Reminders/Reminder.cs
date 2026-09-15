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

    /// <summary>None for a one-off reminder. Otherwise, RemindAtUtc is both the next occurrence and the recurrence anchor.</summary>
    public RecurrenceFrequency RecurrenceFrequency { get; set; } = RecurrenceFrequency.None;

    /// <summary>Repeat every N units of <see cref="RecurrenceFrequency"/> (e.g. 2 + Weekly = every two weeks). Ignored when frequency is None.</summary>
    public int RecurrenceInterval { get; set; } = 1;

    /// <summary>Optional last date the recurrence may fire on. Null repeats indefinitely.</summary>
    public DateTime? RecurrenceEndUtc { get; set; }

    public bool IsRecurring => RecurrenceFrequency != RecurrenceFrequency.None;

    public bool IsPending => Status == ReminderStatus.Pending;

    public bool IsDue(DateTime nowUtc) => Status == ReminderStatus.Pending && RemindAtUtc <= nowUtc;
}
