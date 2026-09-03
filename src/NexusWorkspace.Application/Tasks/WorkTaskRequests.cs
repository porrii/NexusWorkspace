using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Tasks;

public sealed record CreateWorkTaskRequest
{
    public required Guid ProjectId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? DueDateUtc { get; init; }

    public Guid? AssigneePersonId { get; init; }

    public Guid? RelatedCompanyId { get; init; }
}

public sealed record UpdateWorkTaskDetailsRequest
{
    public required Guid Id { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public Priority? Priority { get; init; }

    public bool ClearDueDate { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public bool ChangeAssignee { get; init; }

    public Guid? AssigneePersonId { get; init; }

    public bool ChangeRelatedCompany { get; init; }

    public Guid? RelatedCompanyId { get; init; }
}

public sealed record AddDependencyRequest
{
    public required Guid WorkTaskId { get; init; }

    public required Guid DependsOnWorkTaskId { get; init; }

    public DependencyKind Kind { get; init; } = DependencyKind.FinishToStart;
}
