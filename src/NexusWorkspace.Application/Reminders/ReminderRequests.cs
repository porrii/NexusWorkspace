using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Reminders;

public sealed record CreateReminderRequest
{
    public string Text { get; init; } = string.Empty;

    public DateTime RemindAtUtc { get; init; }

    public EntityKind? TargetKind { get; init; }

    public Guid? TargetId { get; init; }
}
