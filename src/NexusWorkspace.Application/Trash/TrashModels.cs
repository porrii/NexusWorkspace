using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Trash;

public enum TrashScope
{
    Trashed = 0,
    Archived = 1,
    Both = 2,
}

/// <summary>One archived or trashed entity, for the Papelera y archivados screen.</summary>
public sealed record TrashEntry
{
    public required EntityKind Kind { get; init; }

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Context { get; init; }

    public bool IsArchived { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime WhenUtc { get; init; }
}
