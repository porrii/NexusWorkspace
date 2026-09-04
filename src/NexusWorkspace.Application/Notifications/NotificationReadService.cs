using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Notifications;

public sealed record NotificationView
{
    public required Guid Id { get; init; }

    public NotificationKind Kind { get; init; }

    public required string Title { get; init; }

    public string? Body { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public bool IsRead { get; init; }

    public EntityKind? TargetKind { get; init; }

    public Guid? TargetId { get; init; }

    public Guid? ProjectId { get; init; }
}

public sealed class NotificationReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<NotificationView>> GetRecentAsync(
        int limit = 40,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.AsNoTracking().Where(n => !n.IsDismissed);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(n => new NotificationView
            {
                Id = n.Id,
                Kind = n.Kind,
                Title = n.Title,
                Body = n.Body,
                CreatedAtUtc = n.CreatedAtUtc,
                IsRead = n.IsRead,
                TargetKind = n.TargetKind,
                TargetId = n.TargetId,
                ProjectId = n.ProjectId,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default)
        => db.Notifications.AsNoTracking().CountAsync(n => !n.IsRead && !n.IsDismissed, cancellationToken);
}
