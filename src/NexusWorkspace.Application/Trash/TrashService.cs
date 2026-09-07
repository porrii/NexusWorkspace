using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Trash;

/// <summary>
/// Restores archived / trashed entities and, separately, permanently purges them.
/// Purge is a physical delete (cascades to children); callers double-confirm and
/// take a backup first.
/// </summary>
public sealed class TrashService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result> RestoreAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(kind, id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.IsDeleted = false;
        entity.DeletedAtUtc = null;
        entity.IsArchived = false;
        entity.ArchivedAtUtc = null;

        activity.Record(kind, id, ActivityType.RestoredFromTrash, "Restaurado desde la papelera / archivados.");
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> PurgeAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(kind, id, cancellationToken);
        if (entity is null)
        {
            return Result.Success();
        }

        // History rows are append-only and intentionally survive a purge as a tombstone.
        activity.Record(kind, id, ActivityType.Deleted, "Eliminado permanentemente.");
        await db.SaveChangesAsync(cancellationToken);

        Remove(kind, entity);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<int> EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        var count = 0;
        count += await PurgeAllAsync(db.WorkTasks, EntityKind.WorkTask, cancellationToken);
        count += await PurgeAllAsync(db.Projects, EntityKind.Project, cancellationToken);
        count += await PurgeAllAsync(db.People, EntityKind.Person, cancellationToken);
        count += await PurgeAllAsync(db.Companies, EntityKind.Company, cancellationToken);
        return count;
    }

    private async Task<int> PurgeAllAsync<T>(DbSet<T> set, EntityKind kind, CancellationToken cancellationToken)
        where T : AuditableEntity
    {
        var rows = await set.IgnoreQueryFilters().Where(e => e.IsDeleted).ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            activity.Record(kind, row.Id, ActivityType.Deleted, "Eliminado permanentemente (vaciar papelera).");
        }

        set.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private Task<AuditableEntity?> FindAsync(EntityKind kind, Guid id, CancellationToken cancellationToken) => kind switch
    {
        EntityKind.Project => Cast(db.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)),
        EntityKind.WorkTask => Cast(db.WorkTasks.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)),
        EntityKind.Person => Cast(db.People.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)),
        EntityKind.Company => Cast(db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)),
        _ => Task.FromResult<AuditableEntity?>(null),
    };

    private void Remove(EntityKind kind, AuditableEntity entity)
    {
        switch (kind)
        {
            case EntityKind.Project:
                db.Projects.Remove((Domain.Projects.Project)entity);
                break;
            case EntityKind.WorkTask:
                db.WorkTasks.Remove((Domain.Tasks.WorkTask)entity);
                break;
            case EntityKind.Person:
                db.People.Remove((Domain.People.Person)entity);
                break;
            case EntityKind.Company:
                db.Companies.Remove((Domain.Companies.Company)entity);
                break;
        }
    }

    private static async Task<AuditableEntity?> Cast<T>(Task<T?> task) where T : AuditableEntity
        => await task.ConfigureAwait(false);

    private static Result NotFound() => Result.Failure("trash.not_found", "El elemento ya no existe.");
}
