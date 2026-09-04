using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Domain.Tags;

/// <summary>A filterable label, e.g. SIP, DESA, PRE, PRO, URGENTE, INCIDENCIA.</summary>
public class Tag : AuditableEntity
{
    public required string Name { get; set; }

    /// <summary>Hex colour; falls back to a deterministic colour from the name when null.</summary>
    public string? Color { get; set; }

    /// <summary>Optional one-line meaning, shown as a tooltip in the filter bar.</summary>
    public string? Description { get; set; }

    /// <summary>Pinned tags lead the filter bar across every list.</summary>
    public bool IsPinned { get; set; }

    public ICollection<ProjectTag> Projects { get; } = new List<ProjectTag>();

    public ICollection<WorkTaskTag> Tasks { get; } = new List<WorkTaskTag>();

    public ICollection<PersonTag> People { get; } = new List<PersonTag>();

    public ICollection<CompanyTag> Companies { get; } = new List<CompanyTag>();
}
