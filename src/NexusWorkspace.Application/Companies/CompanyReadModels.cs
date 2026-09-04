using NexusWorkspace.Application.People;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Companies;

/// <summary>Row in the Empresas list and in project "companies" sections.</summary>
public sealed record CompanyListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public CompanyKind Kind { get; init; }

    public string? Website { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsArchived { get; init; }

    public DateTime? LastContactedUtc { get; init; }

    public int PeopleCount { get; init; }

    public int ProjectCount { get; init; }

    public IReadOnlyList<TagChip> Tags { get; init; } = [];
}

/// <summary>Header + aggregated counters for the company detail screen.</summary>
public sealed record CompanyDetail
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public CompanyKind Kind { get; init; }

    public string? Website { get; init; }

    public string? Notes { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsArchived { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? LastContactedUtc { get; init; }

    public IReadOnlyList<TagChip> Tags { get; init; } = [];

    public int PeopleCount { get; init; }

    public int ProjectCount { get; init; }

    public int OpenTaskCount { get; init; }

    public int OpenFollowUpCount { get; init; }

    public int CommunicationCount { get; init; }
}

public enum CompanyScope
{
    Active = 0,
    Archived = 1,
    All = 2,
}
