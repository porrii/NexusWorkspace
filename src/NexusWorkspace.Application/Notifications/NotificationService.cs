using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Notifications;

namespace NexusWorkspace.Application.Notifications;

/// <summary>
/// Persists notifications to the local centre and raises an on-screen toast.
/// Also handles read/dismiss state for the shell "bell".
/// </summary>
public sealed class NotificationService(IApplicationDbContext db, IClock clock, IAppNotifier notifier) : INotificationService
{
    public async Task PublishAsync(
        NotificationKind kind,
        string title,
        string? body = null,
        EntityKind? targetKind = null,
        Guid? targetId = null,
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        db.Notifications.Add(new Notification
        {
            Kind = kind,
            Title = title,
            Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
            CreatedAtUtc = clock.UtcNow,
            TargetKind = targetKind,
            TargetId = targetId,
            ProjectId = projectId,
        });

        await db.SaveChangesAsync(cancellationToken);
        notifier.Toast(title, body);
    }

    public async Task<int> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (n is null || n.IsRead)
        {
            return 0;
        }

        n.IsRead = true;
        return await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var unread = await db.Notifications.Where(x => !x.IsRead && !x.IsDismissed).ToListAsync(cancellationToken);
        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DismissAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);
        if (n is null)
        {
            return;
        }

        n.IsDismissed = true;
        n.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
