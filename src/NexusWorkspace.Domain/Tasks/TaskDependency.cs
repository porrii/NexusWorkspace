using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>
/// "<see cref="WorkTask"/> depends on <see cref="DependsOnWorkTask"/>."
/// Rendered as a visual relationship between tasks.
/// </summary>
public class TaskDependency : AuditableEntity
{
    public Guid WorkTaskId { get; set; }

    public WorkTask WorkTask { get; set; } = null!;

    public Guid DependsOnWorkTaskId { get; set; }

    public WorkTask DependsOnWorkTask { get; set; } = null!;

    public DependencyKind Kind { get; set; } = DependencyKind.FinishToStart;
}
