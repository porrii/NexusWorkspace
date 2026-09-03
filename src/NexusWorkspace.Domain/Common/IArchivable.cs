namespace NexusWorkspace.Domain.Common;

/// <summary>
/// Archived entities leave the active views but remain fully available in
/// Archivados, global search, history and statistics. Archival is orthogonal to
/// business status and to soft-delete.
/// </summary>
public interface IArchivable
{
    bool IsArchived { get; set; }
    DateTime? ArchivedAtUtc { get; set; }
}
