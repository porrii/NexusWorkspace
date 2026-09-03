namespace NexusWorkspace.Domain.Common;

/// <summary>
/// Base for business entities that carry audit timestamps and participate in the
/// "never lose information" lifecycle (archive + soft-delete). Timestamps are set
/// by an EF interceptor and are always UTC.
/// </summary>
public abstract class AuditableEntity : Entity, ISoftDelete, IArchivable
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
