using NexusWorkspace.Application.Common;

namespace NexusWorkspace.Application.Tags;

/// <summary>A tag plus how many entities of each kind currently use it.</summary>
public sealed record TagListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Color { get; init; }

    public string DisplayColor => string.IsNullOrWhiteSpace(Color) ? TagColors.For(Name) : Color!;

    public string? Description { get; init; }

    public bool IsPinned { get; init; }

    public int ProjectCount { get; init; }

    public int TaskCount { get; init; }

    public int PersonCount { get; init; }

    public int CompanyCount { get; init; }

    public int TotalUses => ProjectCount + TaskCount + PersonCount + CompanyCount;
}

public sealed record CreateTagRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Color { get; init; }

    public string? Description { get; init; }
}

public sealed record UpdateTagRequest
{
    public required Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Color { get; init; }

    public string? Description { get; init; }

    public bool? IsPinned { get; init; }
}
