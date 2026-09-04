using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Relations;

/// <summary>
/// A typed cross-reference between any two entities (project, task, person,
/// company, follow-up…). Enables "related to", "blocks", "duplicates", "mentions"
/// links and the deep-link navigation between them. Directional: the pair is read
/// from <c>From</c> to <c>To</c>, but the UI shows it on both endpoints.
/// </summary>
public class EntityRelation : AuditableEntity
{
    public EntityKind FromKind { get; set; }

    public Guid FromId { get; set; }

    public EntityKind ToKind { get; set; }

    public Guid ToId { get; set; }

    public RelationKind Kind { get; set; } = RelationKind.RelatesTo;

    public string? Note { get; set; }
}
