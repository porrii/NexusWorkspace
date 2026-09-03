using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Activity;

/// <inheritdoc cref="IActivityLog" />
public sealed class ActivityLog(IApplicationDbContext db, IClock clock) : IActivityLog
{
    public void Record(
        EntityKind targetKind,
        Guid targetId,
        ActivityType type,
        string summary,
        Guid? projectId = null,
        string? oldValue = null,
        string? newValue = null,
        string? note = null,
        QuickActionKind? quickAction = null)
    {
        db.ActivityEvents.Add(new ActivityEvent
        {
            TargetKind = targetKind,
            TargetId = targetId,
            ProjectId = projectId,
            Type = type,
            Summary = summary,
            OldValue = oldValue,
            NewValue = newValue,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            QuickAction = quickAction,
            OccurredAtUtc = clock.UtcNow,
            ActorLabel = "yo",
        });
    }
}
