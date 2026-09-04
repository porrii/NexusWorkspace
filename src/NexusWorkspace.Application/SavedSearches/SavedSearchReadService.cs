using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.SavedSearches;

/// <summary>Read-side for saved searches / filter presets.</summary>
public sealed class SavedSearchReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<SavedSearchView>> GetByKindAsync(SavedSearchKind kind, CancellationToken cancellationToken = default)
        => await db.SavedSearches.AsNoTracking()
            .Where(s => s.Kind == kind)
            .OrderByDescending(s => s.IsPinned)
            .ThenBy(s => s.SortKey)
            .ThenBy(s => s.Name)
            .Select(Project())
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SavedSearchView>> GetPinnedAsync(SavedSearchKind kind, CancellationToken cancellationToken = default)
        => await db.SavedSearches.AsNoTracking()
            .Where(s => s.Kind == kind && s.IsPinned)
            .OrderBy(s => s.SortKey)
            .ThenBy(s => s.Name)
            .Select(Project())
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SavedSearchView>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.SavedSearches.AsNoTracking()
            .OrderBy(s => s.Kind)
            .ThenByDescending(s => s.IsPinned)
            .ThenBy(s => s.Name)
            .Select(Project())
            .ToListAsync(cancellationToken);

    private static System.Linq.Expressions.Expression<Func<Domain.SavedSearches.SavedSearch, SavedSearchView>> Project()
        => s => new SavedSearchView
        {
            Id = s.Id,
            Name = s.Name,
            Kind = s.Kind,
            QueryText = s.QueryText,
            FiltersJson = s.FiltersJson,
            IsPinned = s.IsPinned,
            LastRunUtc = s.LastRunUtc,
        };
}
