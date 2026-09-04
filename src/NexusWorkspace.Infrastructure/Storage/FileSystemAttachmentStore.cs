using System.Security.Cryptography;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Infrastructure.Storage;

/// <summary>
/// Content-addressed file store under <see cref="IAppPaths.FilesDirectory"/>. Files
/// are named by their SHA-256 and sharded by the first two hex characters
/// (<c>files/ab/ab34…​.pdf</c>), so identical uploads deduplicate to one blob.
/// </summary>
public sealed class FileSystemAttachmentStore(IAppPaths paths) : IAttachmentStore
{
    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.FilesDirectory);

        var extension = SafeExtension(originalFileName);
        var tempPath = Path.Combine(paths.FilesDirectory, $"~{Guid.NewGuid():N}.tmp");

        long size;
        string hash;

        try
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var temp = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    hasher.AppendData(buffer, 0, read);
                    await temp.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }

                size = temp.Length;
            }

            hash = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();

            var shard = hash[..2];
            var shardDir = Path.Combine(paths.FilesDirectory, shard);
            Directory.CreateDirectory(shardDir);

            var finalPath = Path.Combine(shardDir, hash + extension);
            if (File.Exists(finalPath))
            {
                File.Delete(tempPath);
            }
            else
            {
                File.Move(tempPath, finalPath);
            }

            return new StoredFile($"{shard}/{hash}{extension}", hash, size);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    public string GetAbsolutePath(string relativePath)
        => Path.GetFullPath(Path.Combine(
            paths.FilesDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public bool Exists(string relativePath) => File.Exists(GetAbsolutePath(relativePath));

    public void Delete(string relativePath)
    {
        var absolute = GetAbsolutePath(relativePath);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }
    }

    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || extension.Length > 16)
        {
            return string.Empty;
        }

        return new string(extension.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()).ToLowerInvariant();
    }
}
