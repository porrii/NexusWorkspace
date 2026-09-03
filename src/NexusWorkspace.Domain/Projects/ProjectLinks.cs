using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.Projects;

/// <summary>Join: a project ↔ a person involved in it.</summary>
public class ProjectPerson
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    /// <summary>Free-text role of this person on this project, e.g. "contacto técnico".</summary>
    public string? Role { get; set; }

    public DateTime LinkedAtUtc { get; set; }
}

/// <summary>Join: a project ↔ a company involved in it.</summary>
public class ProjectCompany
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public DateTime LinkedAtUtc { get; set; }
}

/// <summary>Join: a project ↔ a tag.</summary>
public class ProjectTag
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
