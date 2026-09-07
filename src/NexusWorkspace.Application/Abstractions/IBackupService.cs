namespace NexusWorkspace.Application.Abstractions;

/// <summary>Metadata for one backup archive under the workspace <c>backups/</c> folder.</summary>
public sealed record BackupInfo
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string Reason { get; init; } = "manual";

    public long SizeBytes { get; init; }

    public bool IncludesFiles { get; init; }
}

/// <summary>
/// Creates and restores <c>.zip</c> snapshots of the workspace (database + settings,
/// optionally the attachment store). Restores and full imports are staged to a
/// marker file and applied on the next startup, before the database opens.
/// </summary>
public interface IBackupService
{
    Task<BackupInfo> CreateAsync(string reason, bool includeFiles = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes the oldest archives beyond <paramref name="keep"/>. Returns how many were removed.</summary>
    Task<int> PruneAsync(int keep, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Marks a backup to be restored on the next launch. The app should then close.</summary>
    Task StageRestoreAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Writes a fresh backup archive to an arbitrary path (workspace export).</summary>
    Task<string> ExportToAsync(string destinationPath, bool includeFiles, CancellationToken cancellationToken = default);

    /// <summary>Marks an external <c>.zip</c> to replace the whole workspace on the next launch.</summary>
    Task StageImportAsync(string zipPath, CancellationToken cancellationToken = default);

    /// <summary>Applies a staged restore/import if one is pending. Call once at startup before the DB opens.</summary>
    Task<bool> ApplyStagedAsync(CancellationToken cancellationToken = default);

    /// <summary>True once a restore/import has been staged this session.</summary>
    bool RestartPending { get; }
}
