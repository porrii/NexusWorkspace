using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.People;

/// <summary>Join: a person ↔ a tag.</summary>
public class PersonTag
{
    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
