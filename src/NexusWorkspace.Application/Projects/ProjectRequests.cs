using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Projects;

public sealed record CreateProjectRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public string? Color { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? StartDateUtc { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public Guid? OwnerPersonId { get; init; }
}

public sealed record UpdateProjectDetailsRequest
{
    public required Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public string? Color { get; init; }

    public Priority? Priority { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public Guid? OwnerPersonId { get; init; }
}
