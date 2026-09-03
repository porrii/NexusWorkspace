using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Activity;

/// <summary>
/// One immutable entry in the permanent history. Append-only: never updated, never
/// deleted, never archived — it deliberately does not implement <see cref="ISoftDelete"/>.
/// Generated automatically by EF interceptors and by quick actions. The user never
/// edits it and never types a date.
/// </summary>
public class ActivityEvent : Entity
{
    public EntityKind TargetKind { get; set; }

    public Guid TargetId { get; set; }

    /// <summary>Denormalised project reference so a project timeline is a single indexed query.</summary>
    public Guid? ProjectId { get; set; }

    public ActivityType Type { get; set; }

    /// <summary>Human-readable, localized when written, e.g. "Prioridad modificada de Media a Alta".</summary>
    public required string Summary { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    /// <summary>Optional short note the user added when triggering a quick action.</summary>
    public string? Note { get; set; }

    /// <summary>Set when <see cref="Type"/> is <see cref="ActivityType.QuickAction"/>.</summary>
    public QuickActionKind? QuickAction { get; set; }

    public string ActorLabel { get; set; } = "yo";

    public DateTime OccurredAtUtc { get; set; }
}
