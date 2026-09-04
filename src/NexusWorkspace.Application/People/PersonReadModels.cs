using NexusWorkspace.Application.Common;

namespace NexusWorkspace.Application.People;

/// <summary>A tag chip shown on a person or company row.</summary>
public sealed record TagChip
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Color { get; init; }

    /// <summary>Explicit colour, or a deterministic fallback from the name.</summary>
    public string DisplayColor => string.IsNullOrWhiteSpace(Color) ? TagColors.For(Name) : Color!;
}

/// <summary>Row in the Personas list and in company / project "people" sections.</summary>
public sealed record PersonListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public Guid? CompanyId { get; init; }

    public string? CompanyName { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsArchived { get; init; }

    public DateTime? LastContactedUtc { get; init; }

    public int OpenTaskCount { get; init; }

    public int ProjectCount { get; init; }

    public IReadOnlyList<TagChip> Tags { get; init; } = [];
}

/// <summary>Header + aggregated counters for the person detail screen.</summary>
public sealed record PersonDetail
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Notes { get; init; }

    public Guid? CompanyId { get; init; }

    public string? CompanyName { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsArchived { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? LastContactedUtc { get; init; }

    public IReadOnlyList<TagChip> Tags { get; init; } = [];

    public int ProjectCount { get; init; }

    public int OpenTaskCount { get; init; }

    public int OpenFollowUpCount { get; init; }

    public int CommunicationCount { get; init; }

    public int MeetingCount { get; init; }
}

public enum PersonScope
{
    Active = 0,
    Archived = 1,
    All = 2,
}
