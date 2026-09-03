using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Collaboration;

/// <summary>
/// Metadata for a file stored on disk by the attachment store (never in the DB).
/// Can be attached to any entity.
/// </summary>
public class Attachment : AuditableEntity
{
    public EntityKind TargetKind { get; set; }

    public Guid TargetId { get; set; }

    public Guid? ProjectId { get; set; }

    public required string FileName { get; set; }

    /// <summary>Path relative to the workspace <c>files/</c> root.</summary>
    public required string RelativePath { get; set; }

    public string? MimeType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>SHA-256 of the content, used to deduplicate identical files.</summary>
    public string? ContentHash { get; set; }

    /// <summary>Relative path of a generated thumbnail, when the file type allows one.</summary>
    public string? ThumbnailPath { get; set; }
}
