using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Reminders;

public sealed record ReminderView
{
    public required Guid Id { get; init; }

    public required string Text { get; init; }

    public DateTime RemindAtUtc { get; init; }

    public ReminderStatus Status { get; init; }

    public EntityKind? TargetKind { get; init; }

    public Guid? TargetId { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }
}

public sealed class ReminderReadService(IApplicationDbContext db)
{
    public Task<IReadOnlyList<ReminderView>> GetPendingAsync(CancellationToken cancellationToken = default)
        => RunAsync(db.Reminders.AsNoTracking().Where(r => r.Status == ReminderStatus.Pending)
            .OrderBy(r => r.RemindAtUtc), cancellationToken);

    public Task<IReadOnlyList<ReminderView>> GetForEntityAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
        => RunAsync(db.Reminders.AsNoTracking().Where(r => r.TargetKind == kind && r.TargetId == id)
            .OrderBy(r => r.RemindAtUtc), cancellationToken);

    /// <summary>Pending reminders that are due and have not raised a notification yet. For the scheduler.</summary>
    public Task<IReadOnlyList<ReminderView>> GetDueUnnotifiedAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        => RunAsync(db.Reminders.AsNoTracking()
            .Where(r => r.Status == ReminderStatus.Pending && !r.Notified && r.RemindAtUtc <= nowUtc)
            .OrderBy(r => r.RemindAtUtc), cancellationToken);

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
        => db.Reminders.AsNoTracking().CountAsync(r => r.Status == ReminderStatus.Pending, cancellationToken);

    private static async Task<IReadOnlyList<ReminderView>> RunAsync(IQueryable<Domain.Reminders.Reminder> query, CancellationToken cancellationToken)
    {
        return await query
            .Select(r => new ReminderView
            {
                Id = r.Id,
                Text = r.Text,
                RemindAtUtc = r.RemindAtUtc,
                Status = r.Status,
                TargetKind = r.TargetKind,
                TargetId = r.TargetId,
                ProjectId = r.ProjectId,
                ProjectName = r.Project != null ? r.Project.Name : null,
            })
            .ToListAsync(cancellationToken);
    }
}
