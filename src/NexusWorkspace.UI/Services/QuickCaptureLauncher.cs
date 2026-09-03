using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.UI.ViewModels.QuickCapture;
using NexusWorkspace.UI.Views;

namespace NexusWorkspace.UI.Services;

/// <inheritdoc cref="IQuickCaptureLauncher" />
public sealed class QuickCaptureLauncher(IServiceProvider services) : IQuickCaptureLauncher
{
    private QuickCaptureWindow? _window;

    public void Toggle() => Dispatcher.UIThread.Post(() =>
    {
        EnsureWindow();

        if (_window!.IsVisible)
        {
            _window.Hide();
        }
        else
        {
            _ = _window.PrepareAndShowAsync();
        }
    });

    public void Show() => Dispatcher.UIThread.Post(() =>
    {
        EnsureWindow();
        _ = _window!.PrepareAndShowAsync();
    });

    private void EnsureWindow()
    {
        _window ??= new QuickCaptureWindow
        {
            DataContext = services.GetRequiredService<QuickCaptureViewModel>(),
        };
    }
}
