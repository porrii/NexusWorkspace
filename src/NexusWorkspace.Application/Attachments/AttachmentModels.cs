using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Attachments;

/// <summary>One file attached to an entity, ready to show in a grid or the Archivos page.</summary>
public sealed record AttachmentListItem
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required string RelativePath { get; init; }

    public required string AbsolutePath { get; init; }

    public string? MimeType { get; init; }

    public long SizeBytes { get; init; }

    public AttachmentKind Kind { get; init; }

    public bool IsImage { get; init; }

    public EntityKind TargetKind { get; init; }

    public Guid TargetId { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string HumanSize => FileKinds.HumanSize(SizeBytes);
}

/// <summary>Filter for the global Archivos page.</summary>
public sealed record AttachmentFilter
{
    public Guid? ProjectId { get; init; }

    public AttachmentKind? Kind { get; init; }

    public string? Search { get; init; }

    public bool ImagesOnly { get; init; }
}
