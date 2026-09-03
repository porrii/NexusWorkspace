using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Infrastructure.Search;

/// <summary>
/// Keeps the <c>SearchIndex</c> FTS5 table in sync. On save, for every added or
/// modified project, task or comment it replaces that row's index entry (or
/// removes it when the row was trashed). Archived rows stay searchable.
/// </summary>
public sealed class SearchIndexInterceptor : SaveChangesInterceptor
{
    private static readonly ConditionalWeakTable<DbContext, List<Ref>> Pending = new();

    private readonly record struct Ref(string Kind, string Id);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ApplyAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var refs = new List<Ref>();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Project p:
                    refs.Add(new Ref("Project", p.Id.ToString()));
                    break;
                case WorkTask t:
                    refs.Add(new Ref("WorkTask", t.Id.ToString()));
                    break;
                case Comment c:
                    refs.Add(new Ref("Comment", c.Id.ToString()));
                    break;
            }
        }

        Pending.Remove(context);
        if (refs.Count > 0)
        {
            Pending.Add(context, refs);
        }
    }

    private static async Task ApplyAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !Pending.TryGetValue(context, out var refs))
        {
            return;
        }

        Pending.Remove(context);

        // The search index is best-effort. A problem updating it must never fail a
        // user's save — the index can always be rebuilt from Settings.
        try
        {
            foreach (var reference in refs)
            {
                var kind = reference.Kind;
                var id = reference.Id;

                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM SearchIndex WHERE entity_kind = {kind} AND entity_id = {id}",
                    cancellationToken).ConfigureAwait(false);

                var row = BuildRow(context, reference);
                if (row is null)
                {
                    continue;
                }

                var r = row.Value;
                object? projectId = r.ProjectId;

                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO SearchIndex(entity_kind, entity_id, navigate_kind, navigate_id, project_id, title, body) VALUES ({r.Kind}, {r.Id}, {r.NavKind}, {r.NavId}, {projectId}, {r.Title}, {r.Body})",
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallowed on purpose; see comment above.
        }
    }

    private static IndexRow? BuildRow(DbContext context, Ref reference)
    {
        switch (reference.Kind)
        {
            case "Project":
            {
                var project = Find<Project>(context, reference.Id);
                if (project is null || project.IsDeleted)
                {
                    return null;
                }

                return new IndexRow("Project", project.Id.ToString(), "Project", project.Id.ToString(),
                    null, project.Name, project.Description ?? string.Empty);
            }

            case "WorkTask":
            {
                var task = Find<WorkTask>(context, reference.Id);
                if (task is null || task.IsDeleted)
                {
                    return null;
                }

                return new IndexRow("WorkTask", task.Id.ToString(), "WorkTask", task.Id.ToString(),
                    task.ProjectId.ToString(), task.Title, task.Description ?? string.Empty);
            }

            case "Comment":
            {
                var comment = Find<Comment>(context, reference.Id);
                if (comment is null || comment.IsDeleted)
                {
                    return null;
                }

                return new IndexRow("Comment", comment.Id.ToString(), comment.TargetKind.ToString(),
                    comment.TargetId.ToString(), comment.ProjectId?.ToString(), string.Empty, comment.Body);
            }

            default:
                return null;
        }
    }

    private static TEntity? Find<TEntity>(DbContext context, string id)
        where TEntity : Entity
        => context.ChangeTracker.Entries<TEntity>()
            .Select(e => e.Entity)
            .FirstOrDefault(e => e.Id.ToString() == id);

    private readonly record struct IndexRow(
        string Kind,
        string Id,
        string NavKind,
        string NavId,
        string? ProjectId,
        string Title,
        string Body);
}
