using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Tasks;

/// <summary>Row shown in a task list (project tasks, dashboard, search results).</summary>
public sealed record WorkTaskListItem
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required Guid ProjectId { get; init; }

    public required string ProjectName { get; init; }

    public string? ProjectColor { get; init; }

    public WorkTaskStatus Status { get; init; }

    public Priority Priority { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public DateTime? CompletedDateUtc { get; init; }

    public string? AssigneeName { get; init; }

    public string? RelatedCompanyName { get; init; }

    public int SubTaskTotal { get; init; }

    public int SubTaskDone { get; init; }

    public int ChecklistTotal { get; init; }

    public int ChecklistDone { get; init; }

    public bool IsArchived { get; init; }

    public bool IsFavorite { get; init; }

    public double SortKey { get; init; }

    public bool IsOpen => Status is not (WorkTaskStatus.Finished or WorkTaskStatus.Cancelled);

    public bool IsWaiting => Status is WorkTaskStatus.WaitingClient or WorkTaskStatus.WaitingProvider;

    public int SubTaskProgressPercent => SubTaskTotal == 0 ? 0 : (int)Math.Round(100.0 * SubTaskDone / SubTaskTotal);

    public int ChecklistProgressPercent => ChecklistTotal == 0 ? 0 : (int)Math.Round(100.0 * ChecklistDone / ChecklistTotal);
}

public sealed record SubTaskNode
{
    public required Guid Id { get; init; }

    public Guid? ParentSubTaskId { get; init; }

    public required string Title { get; init; }

    public bool IsDone { get; init; }

    public double SortKey { get; init; }
}

public sealed record ChecklistItemView(Guid Id, string Text, bool IsChecked, double SortKey);

public sealed record TaskCommentView(Guid Id, string Body, string AuthorLabel, DateTime CreatedAtUtc, DateTime? EditedAtUtc);

public sealed record DependencyView(Guid DependencyId, Guid DependsOnTaskId, string DependsOnTitle, WorkTaskStatus DependsOnStatus, DependencyKind Kind);

/// <summary>Everything the task detail screen needs in one round trip.</summary>
public sealed record WorkTaskDetail
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required Guid ProjectId { get; init; }

    public required string ProjectName { get; init; }

    public WorkTaskStatus Status { get; init; }

    public Priority Priority { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public DateTime? CompletedDateUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public bool IsArchived { get; init; }

    public bool IsFavorite { get; init; }

    public Guid? AssigneePersonId { get; init; }

    public string? AssigneeName { get; init; }

    public Guid? RelatedCompanyId { get; init; }

    public string? RelatedCompanyName { get; init; }

    public IReadOnlyList<SubTaskNode> SubTasks { get; init; } = [];

    public IReadOnlyList<ChecklistItemView> Checklist { get; init; } = [];

    public IReadOnlyList<TaskCommentView> Comments { get; init; } = [];

    public IReadOnlyList<DependencyView> DependsOn { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum TaskListScope
{
    Open = 0,
    All = 1,
    Archived = 2,
}
