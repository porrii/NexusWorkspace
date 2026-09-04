using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Notifications;

/// <summary>
/// One entry in the local notification centre (the shell "bell"). Created by the
/// scheduler (due reminders, follow-up nudges) and by explicit app events. Never
/// leaves the device.
/// </summary>
public class Notification : Entity
{
    public NotificationKind Kind { get; set; }

    public required string Title { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsRead { get; set; }

    public bool IsDismissed { get; set; }

    public EntityKind? TargetKind { get; set; }

    public Guid? TargetId { get; set; }

    public Guid? ProjectId { get; set; }
}
