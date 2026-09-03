namespace NexusWorkspace.Domain.Common;

/// <summary>
/// Logical (soft) delete. Rows are hidden by a global query filter, never removed,
/// and can be restored from the trash. Physical delete is a separate, explicit,
/// double-confirmed operation that also takes a backup first.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
