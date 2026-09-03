using NexusWorkspace.Domain.Common;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>An unlimited-depth breakdown item under a <see cref="WorkTask"/>.</summary>
public class SubTask : AuditableEntity
{
    public Guid WorkTaskId { get; set; }

    public WorkTask WorkTask { get; set; } = null!;

    public Guid? ParentSubTaskId { get; set; }

    public SubTask? Parent { get; set; }

    public ICollection<SubTask> Children { get; } = new List<SubTask>();

    public required string Title { get; set; }

    public bool IsDone { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public double SortKey { get; set; }
}
