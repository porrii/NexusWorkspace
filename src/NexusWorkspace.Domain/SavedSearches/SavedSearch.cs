using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.SavedSearches;

/// <summary>
/// A named, reusable query: free text plus a serialised filter payload, bound to
/// one surface (global search, the projects list, the people list…). Pinned
/// searches show as chips at the top of that surface.
/// </summary>
public class SavedSearch : AuditableEntity
{
    public required string Name { get; set; }

    public SavedSearchKind Kind { get; set; } = SavedSearchKind.Global;

    public string? QueryText { get; set; }

    /// <summary>JSON blob of structured filters (tag ids, statuses, date range…). Opaque to the domain.</summary>
    public string? FiltersJson { get; set; }

    public bool IsPinned { get; set; }

    public double SortKey { get; set; }

    public DateTime? LastRunUtc { get; set; }
}
