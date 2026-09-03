using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Collaboration;

/// <summary>A comment attached to any entity (task, project, incident, person…).</summary>
public class Comment : AuditableEntity
{
    public EntityKind TargetKind { get; set; }

    public Guid TargetId { get; set; }

    /// <summary>Denormalised for fast project-wide comment feeds; null when not project-scoped.</summary>
    public Guid? ProjectId { get; set; }

    public required string Body { get; set; }

    /// <summary>Who wrote it. Defaults to the local user; kept for a future multi-user world.</summary>
    public string AuthorLabel { get; set; } = "yo";

    public DateTime? EditedAtUtc { get; set; }
}
