using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.SavedSearches;

namespace NexusWorkspace.Application.SavedSearches;

/// <summary>Write-side for named, reusable searches and filter presets.</summary>
public sealed class SavedSearchService(IApplicationDbContext db, IClock clock)
{
    public async Task<Result<Guid>> SaveAsync(SaveSearchRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("savedsearch.name_required", "Ponle un nombre a la búsqueda.");
        }

        var existing = await db.SavedSearches
            .FirstOrDefaultAsync(s => s.Kind == request.Kind && s.Name == name, cancellationToken);

        if (existing is not null)
        {
            existing.QueryText = Clean(request.QueryText);
            existing.FiltersJson = Clean(request.FiltersJson);
            existing.IsPinned = request.IsPinned;
            await db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var nextSort = await db.SavedSearches.Where(s => s.Kind == request.Kind)
            .Select(s => (double?)s.SortKey).MaxAsync(cancellationToken) ?? 0d;

        var saved = new SavedSearch
        {
            Name = name,
            Kind = request.Kind,
            QueryText = Clean(request.QueryText),
            FiltersJson = Clean(request.FiltersJson),
            IsPinned = request.IsPinned,
            SortKey = nextSort + 1d,
        };

        db.SavedSearches.Add(saved);
        await db.SaveChangesAsync(cancellationToken);
        return saved.Id;
    }

    public async Task<Result> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saved is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            saved.Name = name.Trim();
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> SetPinnedAsync(Guid id, bool pinned, CancellationToken cancellationToken = default)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saved is null)
        {
            return NotFound();
        }

        saved.IsPinned = pinned;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> TouchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saved is null)
        {
            return NotFound();
        }

        saved.LastRunUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (saved is null)
        {
            return Result.Success();
        }

        db.SavedSearches.Remove(saved);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("savedsearch.not_found", "Búsqueda guardada no encontrada.");
}
