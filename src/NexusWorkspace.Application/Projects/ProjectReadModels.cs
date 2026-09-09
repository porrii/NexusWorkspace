using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.People;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Projects;

/// <summary>Row shown in the Proyectos list and the dashboard "active projects" widget.</summary>
public sealed record ProjectListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public string? Color { get; init; }

    public ProjectStatus Status { get; init; }

    public Priority Priority { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsArchived { get; init; }

    public int TotalTaskCount { get; init; }

    public int OpenTaskCount { get; init; }

    public int FinishedTaskCount { get; init; }

    public DateTime? LastActivityUtc { get; init; }

    public int ProgressPercent => TotalTaskCount == 0
        ? 0
        : (int)Math.Round(100.0 * FinishedTaskCount / TotalTaskCount);
}

/// <summary>Header data for the project detail screen.</summary>
public sealed record ProjectDetail
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public string? Color { get; init; }

    public ProjectStatus Status { get; init; }

    public Priority Priority { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public DateTime? CompletedDateUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public bool IsArchived { get; init; }

    public bool IsFavorite { get; init; }

    public Guid? OwnerPersonId { get; init; }

    public string? OwnerName { get; init; }

    public int TotalTaskCount { get; init; }

    public int OpenTaskCount { get; init; }

    public int FinishedTaskCount { get; init; }

    public IReadOnlyList<TagChip> Tags { get; init; } = [];

    public IReadOnlyList<PersonListItem> Team { get; init; } = [];

    public IReadOnlyList<CompanyListItem> Companies { get; init; } = [];

    public int ProgressPercent => TotalTaskCount == 0
        ? 0
        : (int)Math.Round(100.0 * FinishedTaskCount / TotalTaskCount);
}

public enum ProjectListScope
{
    Active = 0,
    Archived = 1,
    All = 2,
}
