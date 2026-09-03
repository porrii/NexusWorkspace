using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Common;

namespace NexusWorkspace.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets audit and lifecycle timestamps on save: <c>CreatedAtUtc</c>/<c>UpdatedAtUtc</c>
/// always, and <c>ArchivedAtUtc</c>/<c>DeletedAtUtc</c> whenever the corresponding
/// flag flips. Activity-log entries themselves are written explicitly by the
/// application services, not here.
/// </summary>
public sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    if (entry.Entity.IsArchived)
                    {
                        entry.Entity.ArchivedAtUtc ??= now;
                    }

                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;

                    if (entry.Property(nameof(IArchivable.IsArchived)).IsModified)
                    {
                        entry.Entity.ArchivedAtUtc = entry.Entity.IsArchived ? now : null;
                    }

                    if (entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified)
                    {
                        entry.Entity.DeletedAtUtc = entry.Entity.IsDeleted ? now : null;
                    }

                    break;
            }
        }
    }
}
