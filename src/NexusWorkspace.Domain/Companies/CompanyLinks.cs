using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Domain.Companies;

/// <summary>Join: a company ↔ a tag.</summary>
public class CompanyTag
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
