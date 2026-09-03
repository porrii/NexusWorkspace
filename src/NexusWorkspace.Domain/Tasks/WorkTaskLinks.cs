using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>Join: a task ↔ a tag.</summary>
public class WorkTaskTag
{
    public Guid WorkTaskId { get; set; }

    public WorkTask WorkTask { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}

/// <summary>Join: a task ↔ a related person (beyond the single assignee).</summary>
public class WorkTaskPerson
{
    public Guid WorkTaskId { get; set; }

    public WorkTask WorkTask { get; set; } = null!;

    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public DateTime LinkedAtUtc { get; set; }
}
