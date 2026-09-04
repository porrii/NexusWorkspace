using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Application.Tags;

/// <summary>Read-side for tags, including per-kind usage counts for the Etiquetas page.</summary>
public sealed class TagReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<TagListItem>> GetAllAsync(bool withCounts = true, CancellationToken cancellationToken = default)
    {
        if (!withCounts)
        {
            return await db.Tags.AsNoTracking()
                .OrderByDescending(t => t.IsPinned)
                .ThenBy(t => t.Name)
                .Select(t => new TagListItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    Color = t.Color,
                    Description = t.Description,
                    IsPinned = t.IsPinned,
                })
                .ToListAsync(cancellationToken);
        }

        return await db.Tags.AsNoTracking()
            .OrderByDescending(t => t.IsPinned)
            .ThenBy(t => t.Name)
            .Select(t => new TagListItem
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                Description = t.Description,
                IsPinned = t.IsPinned,
                ProjectCount = db.Projects.Count(p => p.Tags.Any(x => x.TagId == t.Id)),
                TaskCount = db.WorkTasks.Count(w => w.Tags.Any(x => x.TagId == t.Id)),
                PersonCount = db.People.Count(p => p.Tags.Any(x => x.TagId == t.Id)),
                CompanyCount = db.Companies.Count(c => c.Tags.Any(x => x.TagId == t.Id)),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TagListItem>> GetPinnedAsync(CancellationToken cancellationToken = default)
        => GetFilteredAsync(pinnedOnly: true, cancellationToken);

    private async Task<IReadOnlyList<TagListItem>> GetFilteredAsync(bool pinnedOnly, CancellationToken cancellationToken)
    {
        var query = db.Tags.AsNoTracking().AsQueryable();
        if (pinnedOnly)
        {
            query = query.Where(t => t.IsPinned);
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TagListItem
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                Description = t.Description,
                IsPinned = t.IsPinned,
            })
            .ToListAsync(cancellationToken);
    }
}
