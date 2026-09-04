using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.People;

/// <summary>
/// A person you work with. Their detail view aggregates every project, task,
/// follow-up, communication and meeting they are linked to.
/// </summary>
public class Person : AuditableEntity
{
    public required string Name { get; set; }

    public string? Role { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public bool IsFavorite { get; set; }

    /// <summary>Denormalised from the newest logged communication; drives "not contacted in a while".</summary>
    public DateTime? LastContactedUtc { get; set; }

    public ICollection<PersonTag> Tags { get; } = new List<PersonTag>();
}
