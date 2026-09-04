using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Publishes a notification: persists it to the local notification centre and
/// raises an on-screen toast. Everything stays on the device.
/// </summary>
public interface INotificationService
{
    Task PublishAsync(
        NotificationKind kind,
        string title,
        string? body = null,
        EntityKind? targetKind = null,
        Guid? targetId = null,
        Guid? projectId = null,
        CancellationToken cancellationToken = default);
}
