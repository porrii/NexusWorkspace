using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.SavedSearches;

public sealed record SavedSearchView
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public SavedSearchKind Kind { get; init; }

    public string? QueryText { get; init; }

    public string? FiltersJson { get; init; }

    public bool IsPinned { get; init; }

    public DateTime? LastRunUtc { get; init; }
}

public sealed record SaveSearchRequest
{
    public string Name { get; init; } = string.Empty;

    public SavedSearchKind Kind { get; init; } = SavedSearchKind.Global;

    public string? QueryText { get; init; }

    public string? FiltersJson { get; init; }

    public bool IsPinned { get; init; }
}
