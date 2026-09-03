using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>
/// A task ("Tarea" in the UI). Named <c>WorkTask</c> to avoid colliding with
/// <see cref="System.Threading.Tasks.Task"/> in an async codebase.
/// </summary>
public class WorkTask : AuditableEntity
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Pending;

    public Priority Priority { get; set; } = Priority.Medium;

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime? CompletedDateUtc { get; set; }

    public Guid? AssigneePersonId { get; set; }

    public Person? Assignee { get; set; }

    public Guid? RelatedCompanyId { get; set; }

    public Company? RelatedCompany { get; set; }

    /// <summary>Fractional sort key so a task can be reordered without touching its neighbours.</summary>
    public double SortKey { get; set; }

    public bool IsFavorite { get; set; }

    public ICollection<SubTask> SubTasks { get; } = new List<SubTask>();

    public ICollection<ChecklistItem> Checklist { get; } = new List<ChecklistItem>();

    public ICollection<WorkTaskTag> Tags { get; } = new List<WorkTaskTag>();

    public ICollection<WorkTaskPerson> People { get; } = new List<WorkTaskPerson>();

    /// <summary>Dependencies where this task is the dependent side (this depends on others).</summary>
    public ICollection<TaskDependency> DependsOn { get; } = new List<TaskDependency>();

    /// <summary>Dependencies where this task is depended upon by others.</summary>
    public ICollection<TaskDependency> Dependents { get; } = new List<TaskDependency>();

    public bool IsOpen => Status is not (WorkTaskStatus.Finished or WorkTaskStatus.Cancelled);

    public bool IsWaiting => Status is WorkTaskStatus.WaitingClient or WorkTaskStatus.WaitingProvider;

    public bool IsOverdue =>
        IsOpen && DueDateUtc is { } due && due.Date < DateTime.UtcNow.Date;

    public int ChecklistProgressPercent
        => Checklist.Count == 0
            ? 0
            : (int)Math.Round(100.0 * Checklist.Count(c => c.IsChecked) / Checklist.Count);

    public int SubTaskProgressPercent
        => SubTasks.Count == 0
            ? 0
            : (int)Math.Round(100.0 * SubTasks.Count(s => s.IsDone) / SubTasks.Count);
}
