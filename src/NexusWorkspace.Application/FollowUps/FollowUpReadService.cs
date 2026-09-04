using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.FollowUps;

/// <summary>Row for the "Esperando respuesta" list and the dashboard widget.</summary>
public sealed record FollowUpListItem
{
    public required Guid Id { get; init; }

    public required string Subject { get; init; }

    public EntityKind TargetKind { get; init; }

    public Guid TargetId { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string WaitingOn { get; init; } = "—";

    public FollowUpState State { get; init; }

    public DateTime WaitingSinceUtc { get; init; }

    public DateTime? LastContactUtc { get; init; }

    public DateTime? NextFollowUpUtc { get; init; }

    public int ReminderCount { get; init; }

    public int DaysWaiting { get; init; }

    public int DaysSinceLastContact { get; init; }

    public bool IsOpen => State is FollowUpState.Waiting or FollowUpState.Escalated;
}

public enum FollowUpScope
{
    Open = 0,
    Resolved = 1,
    All = 2,
}

public sealed class FollowUpReadService(IApplicationDbContext db, IClock clock)
{
    public async Task<IReadOnlyList<FollowUpListItem>> GetListAsync(
        FollowUpScope scope = FollowUpScope.Open,
        CancellationToken cancellationToken = default)
    {
        var query = db.FollowUps.AsNoTracking().AsQueryable();

        query = scope switch
        {
            FollowUpScope.Open => query.Where(f => f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated),
            FollowUpScope.Resolved => query.Where(f => f.State == FollowUpState.Answered || f.State == FollowUpState.Closed),
            _ => query,
        };

        var rows = await query
            .OrderBy(f => f.WaitingSinceUtc)
            .Select(f => new
            {
                f.Id,
                f.Subject,
                f.TargetKind,
                f.TargetId,
                f.ProjectId,
                ProjectName = f.Project != null ? f.Project.Name : null,
                f.WaitingOnLabel,
                f.State,
                f.WaitingSinceUtc,
                f.LastContactUtc,
                f.NextFollowUpUtc,
                f.ReminderCount,
            })
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;

        return rows.Select(r => new FollowUpListItem
        {
            Id = r.Id,
            Subject = r.Subject,
            TargetKind = r.TargetKind,
            TargetId = r.TargetId,
            ProjectId = r.ProjectId,
            ProjectName = r.ProjectName,
            WaitingOn = string.IsNullOrWhiteSpace(r.WaitingOnLabel) ? "—" : r.WaitingOnLabel!,
            State = r.State,
            WaitingSinceUtc = r.WaitingSinceUtc,
            LastContactUtc = r.LastContactUtc,
            NextFollowUpUtc = r.NextFollowUpUtc,
            ReminderCount = r.ReminderCount,
            DaysWaiting = Math.Max(0, (int)Math.Floor((now - r.WaitingSinceUtc).TotalDays)),
            DaysSinceLastContact = Math.Max(0, (int)Math.Floor((now - (r.LastContactUtc ?? r.WaitingSinceUtc)).TotalDays)),
        }).ToList();
    }

    public async Task<IReadOnlyList<FollowUpListItem>> GetForEntityAsync(
        EntityKind kind,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var all = await GetListAsync(FollowUpScope.All, cancellationToken);
        return all.Where(f => f.TargetKind == kind && f.TargetId == id).ToList();
    }

    public async Task<IReadOnlyList<FollowUpListItem>> GetWaitingOnPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var ids = await db.FollowUps.AsNoTracking().Where(f => f.WaitingOnPersonId == personId)
            .Select(f => f.Id).ToListAsync(cancellationToken);
        var all = await GetListAsync(FollowUpScope.All, cancellationToken);
        return all.Where(f => ids.Contains(f.Id)).ToList();
    }

    public async Task<IReadOnlyList<FollowUpListItem>> GetWaitingOnCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var ids = await db.FollowUps.AsNoTracking().Where(f => f.WaitingOnCompanyId == companyId)
            .Select(f => f.Id).ToListAsync(cancellationToken);
        var all = await GetListAsync(FollowUpScope.All, cancellationToken);
        return all.Where(f => ids.Contains(f.Id)).ToList();
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
        => db.FollowUps.AsNoTracking()
            .CountAsync(f => f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated, cancellationToken);
}
