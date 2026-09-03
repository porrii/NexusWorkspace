using NexusWorkspace.Domain.Common;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>A quick check item inside a task. Progress is computed on the parent task.</summary>
public class ChecklistItem : AuditableEntity
{
    public Guid WorkTaskId { get; set; }

    public WorkTask WorkTask { get; set; } = null!;

    public required string Text { get; set; }

    public bool IsChecked { get; set; }

    public DateTime? CheckedAtUtc { get; set; }

    public double SortKey { get; set; }
}
