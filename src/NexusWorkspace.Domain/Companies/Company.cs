using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.Companies;

/// <summary>
/// A company (client, provider, internal area…). Its detail view aggregates every
/// project, task, incident, communication and person linked to it.
/// </summary>
public class Company : AuditableEntity
{
    public required string Name { get; set; }

    public CompanyKind Kind { get; set; } = CompanyKind.Other;

    public string? Website { get; set; }

    public string? Notes { get; set; }

    public bool IsFavorite { get; set; }

    /// <summary>Denormalised from the newest logged communication with anyone at the company.</summary>
    public DateTime? LastContactedUtc { get; set; }

    public ICollection<Person> People { get; } = new List<Person>();

    public ICollection<CompanyTag> Tags { get; } = new List<CompanyTag>();
}
