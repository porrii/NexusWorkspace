using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Infrastructure.Backups;

/// <summary>
/// Zip-based backups under <see cref="IAppPaths.BackupsDirectory"/>. Each archive
/// holds the SQLite files, <c>settings.json</c>, a <c>manifest.json</c> and,
/// optionally, the whole attachment store. Restores and full imports are staged
/// to a marker file and applied by <see cref="ApplyStagedAsync"/> at startup,
/// before EF opens the database.
/// </summary>
public sealed class FileSystemBackupService(IAppPaths paths, ILogger<FileSystemBackupService> logger) : IBackupService
{
    private const string MarkerName = ".nexus-restore";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public bool RestartPending { get; private set; }

    public async Task<BackupInfo> CreateAsync(string reason, bool includeFiles = false, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.BackupsDirectory);
        var safeReason = Sanitize(string.IsNullOrWhiteSpace(reason) ? "manual" : reason);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var fileName = $"nexus-{stamp}-{safeReason}.zip";
        var zipPath = Path.Combine(paths.BackupsDirectory, fileName);

        await Task.Run(() => BuildArchive(zipPath, includeFiles, safeReason), cancellationToken).ConfigureAwait(false);

        var info = new FileInfo(zipPath);
        logger.LogInformation("Copia de seguridad creada: {File} ({Bytes} bytes).", fileName, info.Length);

        return new BackupInfo
        {
            Id = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName,
            CreatedAtUtc = DateTime.UtcNow,
            Reason = safeReason,
            SizeBytes = info.Length,
            IncludesFiles = includeFiles,
        };
    }

    public Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(paths.BackupsDirectory))
        {
            return Task.FromResult<IReadOnlyList<BackupInfo>>([]);
        }

        var list = new List<BackupInfo>();
        foreach (var file in Directory.EnumerateFiles(paths.BackupsDirectory, "*.zip"))
        {
            var fi = new FileInfo(file);
            var manifest = TryReadManifest(file);
            list.Add(new BackupInfo
            {
                Id = Path.GetFileNameWithoutExtension(file),
                FileName = Path.GetFileName(file),
                CreatedAtUtc = manifest?.CreatedAtUtc ?? fi.CreationTimeUtc,
                Reason = manifest?.Reason ?? ReasonFromName(fi.Name),
                SizeBytes = fi.Length,
                IncludesFiles = manifest?.IncludesFiles ?? false,
            });
        }

        return Task.FromResult<IReadOnlyList<BackupInfo>>(list.OrderByDescending(b => b.CreatedAtUtc).ToList());
    }

    public async Task<int> PruneAsync(int keep, CancellationToken cancellationToken = default)
    {
        keep = Math.Max(1, keep);
        var all = await ListAsync(cancellationToken).ConfigureAwait(false);
        var toDelete = all.Skip(keep).ToList();
        foreach (var backup in toDelete)
        {
            SafeDelete(Path.Combine(paths.BackupsDirectory, backup.FileName));
        }

        if (toDelete.Count > 0)
        {
            logger.LogInformation("Podadas {Count} copias antiguas (se conservan {Keep}).", toDelete.Count, keep);
        }

        return toDelete.Count;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        SafeDelete(Path.Combine(paths.BackupsDirectory, id + ".zip"));
        return Task.CompletedTask;
    }

    public Task StageRestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        var zipPath = Path.Combine(paths.BackupsDirectory, id + ".zip");
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("La copia de seguridad no existe.", zipPath);
        }

        WriteMarker(zipPath, fullImport: false);
        RestartPending = true;
        return Task.CompletedTask;
    }

    public async Task<string> ExportToAsync(string destinationPath, bool includeFiles, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() => BuildArchive(destinationPath, includeFiles, "export"), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Workspace exportado a {Path}.", destinationPath);
        return destinationPath;
    }

    public Task StageImportAsync(string zipPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("El archivo a importar no existe.", zipPath);
        }

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            if (archive.GetEntry("db/nexus.db") is null)
            {
                throw new InvalidDataException("El archivo no es un workspace de NexusWorkspace.");
            }
        }

        Directory.CreateDirectory(paths.BackupsDirectory);
        var copyPath = Path.Combine(paths.BackupsDirectory, $"nexus-{DateTime.Now:yyyyMMdd-HHmmss}-import.zip");
        File.Copy(zipPath, copyPath, overwrite: true);

        WriteMarker(copyPath, fullImport: true);
        RestartPending = true;
        return Task.CompletedTask;
    }

    public Task<bool> ApplyStagedAsync(CancellationToken cancellationToken = default)
    {
        var markerPath = Path.Combine(paths.RootDirectory, MarkerName);
        if (!File.Exists(markerPath))
        {
            return Task.FromResult(false);
        }

        RestoreMarker? marker;
        try
        {
            marker = JsonSerializer.Deserialize<RestoreMarker>(File.ReadAllText(markerPath), Json);
        }
        catch (JsonException)
        {
            marker = null;
        }

        SafeDelete(markerPath);

        if (marker is null || !File.Exists(marker.ArchivePath))
        {
            logger.LogWarning("Restauración pendiente ignorada: archivo no encontrado.");
            return Task.FromResult(false);
        }

        try
        {
            if (File.Exists(paths.DatabasePath))
            {
                BuildArchive(Path.Combine(paths.BackupsDirectory, $"nexus-{DateTime.Now:yyyyMMdd-HHmmss}-prerestore.zip"), includeFiles: false, "prerestore");
            }

            ExtractArchive(marker.ArchivePath, marker.FullImport);
            logger.LogInformation("Workspace restaurado desde {Archive}.", Path.GetFileName(marker.ArchivePath));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo al aplicar la restauración pendiente. Se continúa con el workspace actual.");
            return Task.FromResult(false);
        }
    }

    // ---------- internals ----------

    private void BuildArchive(string zipPath, bool includeFiles, string reason)
    {
        TryCheckpoint();
        // Release every pooled SQLite handle so the file copy below is not blocked.
        SqliteConnection.ClearAllPools();

        SafeDelete(zipPath);
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var source = paths.DatabasePath + suffix;
            if (File.Exists(source))
            {
                zip.CreateEntryFromFile(source, "db/nexus.db" + suffix);
            }
        }

        if (File.Exists(paths.SettingsFilePath))
        {
            zip.CreateEntryFromFile(paths.SettingsFilePath, "settings.json");
        }

        var fileCount = 0;
        if (includeFiles && Directory.Exists(paths.FilesDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(paths.FilesDirectory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(paths.FilesDirectory, file).Replace('\\', '/');
                zip.CreateEntryFromFile(file, "files/" + relative);
                fileCount++;
            }
        }

        var manifest = new BackupManifest
        {
            CreatedAtUtc = DateTime.UtcNow,
            Reason = reason,
            IncludesFiles = includeFiles,
            FileCount = fileCount,
            DbBytes = File.Exists(paths.DatabasePath) ? new FileInfo(paths.DatabasePath).Length : 0,
        };

        var manifestEntry = zip.CreateEntry("manifest.json");
        using var writer = new StreamWriter(manifestEntry.Open());
        writer.Write(JsonSerializer.Serialize(manifest, Json));
    }

    private void ExtractArchive(string zipPath, bool fullImport)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            SafeDelete(paths.DatabasePath + suffix);
            var entry = archive.GetEntry("db/nexus.db" + suffix);
            entry?.ExtractToFile(paths.DatabasePath + suffix, overwrite: true);
        }

        var settingsEntry = archive.GetEntry("settings.json");
        settingsEntry?.ExtractToFile(paths.SettingsFilePath, overwrite: true);

        if (fullImport)
        {
            if (Directory.Exists(paths.FilesDirectory))
            {
                Directory.Delete(paths.FilesDirectory, recursive: true);
            }

            Directory.CreateDirectory(paths.FilesDirectory);

            foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("files/", StringComparison.Ordinal) && e.Length >= 0 && !e.FullName.EndsWith('/')))
            {
                var target = Path.Combine(paths.FilesDirectory, entry.FullName["files/".Length..].Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }
        }
    }

    private void TryCheckpoint()
    {
        if (!File.Exists(paths.DatabasePath) || !LooksLikeSqlite(paths.DatabasePath))
        {
            return;
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={paths.DatabasePath};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            logger.LogDebug(ex, "No se pudo hacer checkpoint del WAL antes de la copia.");
        }
    }

    private void WriteMarker(string archivePath, bool fullImport)
    {
        var marker = new RestoreMarker
        {
            ArchivePath = archivePath,
            FullImport = fullImport,
            StagedAtUtc = DateTime.UtcNow,
        };
        File.WriteAllText(Path.Combine(paths.RootDirectory, MarkerName), JsonSerializer.Serialize(marker, Json));
    }

    private BackupManifest? TryReadManifest(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry("manifest.json");
            if (entry is null)
            {
                return null;
            }

            using var reader = new StreamReader(entry.Open());
            return JsonSerializer.Deserialize<BackupManifest>(reader.ReadToEnd(), Json);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or JsonException)
        {
            return null;
        }
    }

    private static bool LooksLikeSqlite(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[16];
            return stream.Read(header) == 16
                   && header.SequenceEqual("SQLite format 3\0"u8);
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Ignore; a locked file will be retried next run.
        }
    }

    private static string Sanitize(string value)
    {
        var cleaned = new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(cleaned) ? "manual" : cleaned;
    }

    private static string ReasonFromName(string fileName)
    {
        var parts = Path.GetFileNameWithoutExtension(fileName).Split('-');
        return parts.Length >= 4 ? parts[^1] : "manual";
    }

    private sealed record RestoreMarker
    {
        public required string ArchivePath { get; init; }

        public bool FullImport { get; init; }

        public DateTime StagedAtUtc { get; init; }
    }

    private sealed record BackupManifest
    {
        public DateTime CreatedAtUtc { get; init; }

        public string Reason { get; init; } = "manual";

        public bool IncludesFiles { get; init; }

        public int FileCount { get; init; }

        public long DbBytes { get; init; }
    }
}
