namespace NexusWorkspace.Application.Abstractions;

/// <summary>Result of persisting a file to the external attachment store.</summary>
public sealed record StoredFile(string RelativePath, string ContentHash, long SizeBytes);

/// <summary>
/// The external file store for attachments: files live on disk under the workspace
/// <c>files/</c> directory, never in the database. Content-addressed and
/// deduplicated by SHA-256, so two identical uploads share one physical file.
/// </summary>
public interface IAttachmentStore
{
    /// <summary>Streams <paramref name="content"/> to disk, returning its relative path, hash and size.</summary>
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);

    /// <summary>Absolute path for a stored relative path (uses <c>/</c> separators internally).</summary>
    string GetAbsolutePath(string relativePath);

    bool Exists(string relativePath);

    /// <summary>Deletes the physical file. Callers must first check no live attachment still references it.</summary>
    void Delete(string relativePath);
}
