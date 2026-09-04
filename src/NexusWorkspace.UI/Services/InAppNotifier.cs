using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.UI.Services;

/// <summary>
/// Shows toasts via Avalonia's in-window <see cref="WindowNotificationManager"/>
/// (top-right, auto-dismissing). Attached to the shell once it exists.
/// </summary>
public sealed class InAppNotifier : IAppNotifier
{
    private WindowNotificationManager? _manager;

    public void Attach(TopLevel topLevel)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _manager = new WindowNotificationManager(topLevel)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 4,
            };
        });
    }

    public void Toast(string title, string? body)
    {
        Dispatcher.UIThread.Post(() =>
            _manager?.Show(new Avalonia.Controls.Notifications.Notification(
                title,
                body ?? string.Empty,
                NotificationType.Information)));
    }
}
