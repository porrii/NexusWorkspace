using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Appends an entry to the immutable history. Adds the entity to the context but
/// does <em>not</em> save — the calling service saves once for the whole operation.
/// Timestamps come from <see cref="IClock"/>.
/// </summary>
public interface IActivityLog
{
    void Record(
        EntityKind targetKind,
        Guid targetId,
        ActivityType type,
        string summary,
        Guid? projectId = null,
        string? oldValue = null,
        string? newValue = null,
        string? note = null,
        QuickActionKind? quickAction = null);
}
