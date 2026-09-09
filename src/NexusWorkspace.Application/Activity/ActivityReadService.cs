using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Activity;

/// <summary>One row in an activity feed or a project/task timeline.</summary>
public sealed record ActivityEntry
{
    public required Guid Id { get; init; }

    public EntityKind TargetKind { get; init; }

    public Guid TargetId { get; init; }

    public Guid? ProjectId { get; init; }

    public ActivityType Type { get; init; }

    public required string Summary { get; init; }

    public string? Note { get; init; }

    public QuickActionKind? QuickAction { get; init; }

    public string ActorLabel { get; init; } = "yo";

    public DateTime OccurredAtUtc { get; init; }
}

/// <summary>Filter for the global activity feed. Null members are ignored.</summary>
public sealed record ActivityFilter
{
    public Guid? ProjectId { get; init; }

    public EntityKind? TargetKind { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? Text { get; init; }
}

/// <summary>Read-side access to the immutable history.</summary>
public sealed class ActivityReadService(IApplicationDbContext db)
{
    public Task<IReadOnlyList<ActivityEntry>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
        => RunAsync(db.ActivityEvents.AsNoTracking(), limit, cancellationToken);

    public Task<IReadOnlyList<ActivityEntry>> GetFilteredAsync(ActivityFilter filter, int limit = 250, CancellationToken cancellationToken = default)
    {
        var query = db.ActivityEvents.AsNoTracking();

        if (filter.ProjectId is { } projectId)
        {
            query = query.Where(a => a.ProjectId == projectId);
        }

        if (filter.TargetKind is { } kind)
        {
            query = query.Where(a => a.TargetKind == kind);
        }

        if (filter.FromUtc is { } from)
        {
            query = query.Where(a => a.OccurredAtUtc >= from);
        }

        if (filter.ToUtc is { } to)
        {
            query = query.Where(a => a.OccurredAtUtc < to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            var like = $"%{filter.Text.Trim()}%";
            query = query.Where(a => EF.Functions.Like(a.Summary, like)
                                     || (a.Note != null && EF.Functions.Like(a.Note, like)));
        }

        return RunAsync(query, limit, cancellationToken);
    }

    public Task<IReadOnlyList<ActivityEntry>> GetForProjectAsync(Guid projectId, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.ActivityEvents.AsNoTracking().Where(a => a.ProjectId == projectId), limit, cancellationToken);

    public Task<IReadOnlyList<ActivityEntry>> GetForEntityAsync(EntityKind kind, Guid id, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.ActivityEvents.AsNoTracking().Where(a => a.TargetKind == kind && a.TargetId == id), limit, cancellationToken);

    private static async Task<IReadOnlyList<ActivityEntry>> RunAsync(
        IQueryable<Domain.Activity.ActivityEvent> query,
        int limit,
        CancellationToken cancellationToken)
    {
        return await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(Math.Clamp(limit, 1, 2000))
            .Select(a => new ActivityEntry
            {
                Id = a.Id,
                TargetKind = a.TargetKind,
                TargetId = a.TargetId,
                ProjectId = a.ProjectId,
                Type = a.Type,
                Summary = a.Summary,
                Note = a.Note,
                QuickAction = a.QuickAction,
                ActorLabel = a.ActorLabel,
                OccurredAtUtc = a.OccurredAtUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
