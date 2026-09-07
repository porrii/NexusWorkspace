using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Trash;

/// <summary>Lists archived / soft-deleted projects, tasks, people and companies.</summary>
public sealed class TrashReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<TrashEntry>> GetAsync(TrashScope scope, CancellationToken cancellationToken = default)
    {
        var entries = new List<TrashEntry>();

        entries.AddRange(await db.Projects.AsNoTracking().IgnoreQueryFilters()
            .Where(Predicate<Domain.Projects.Project>(scope))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(1000)
            .Select(p => new TrashEntry
            {
                Kind = EntityKind.Project,
                Id = p.Id,
                Name = p.Name,
                Context = null,
                IsArchived = p.IsArchived,
                IsDeleted = p.IsDeleted,
                WhenUtc = p.DeletedAtUtc ?? p.ArchivedAtUtc ?? p.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken));

        entries.AddRange(await db.WorkTasks.AsNoTracking().IgnoreQueryFilters()
            .Where(Predicate<Domain.Tasks.WorkTask>(scope))
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(1000)
            .Select(t => new TrashEntry
            {
                Kind = EntityKind.WorkTask,
                Id = t.Id,
                Name = t.Title,
                Context = t.Project.Name,
                IsArchived = t.IsArchived,
                IsDeleted = t.IsDeleted,
                WhenUtc = t.DeletedAtUtc ?? t.ArchivedAtUtc ?? t.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken));

        entries.AddRange(await db.People.AsNoTracking().IgnoreQueryFilters()
            .Where(Predicate<Domain.People.Person>(scope))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(1000)
            .Select(p => new TrashEntry
            {
                Kind = EntityKind.Person,
                Id = p.Id,
                Name = p.Name,
                Context = p.Company != null ? p.Company.Name : null,
                IsArchived = p.IsArchived,
                IsDeleted = p.IsDeleted,
                WhenUtc = p.DeletedAtUtc ?? p.ArchivedAtUtc ?? p.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken));

        entries.AddRange(await db.Companies.AsNoTracking().IgnoreQueryFilters()
            .Where(Predicate<Domain.Companies.Company>(scope))
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(1000)
            .Select(c => new TrashEntry
            {
                Kind = EntityKind.Company,
                Id = c.Id,
                Name = c.Name,
                Context = null,
                IsArchived = c.IsArchived,
                IsDeleted = c.IsDeleted,
                WhenUtc = c.DeletedAtUtc ?? c.ArchivedAtUtc ?? c.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken));

        return entries.OrderByDescending(e => e.WhenUtc).ToList();
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> Predicate<T>(TrashScope scope)
        where T : Domain.Common.AuditableEntity
        => scope switch
        {
            TrashScope.Trashed => e => e.IsDeleted,
            TrashScope.Archived => e => e.IsArchived && !e.IsDeleted,
            _ => e => e.IsDeleted || e.IsArchived,
        };
}
