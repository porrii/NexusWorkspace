using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels;
using NexusWorkspace.UI.Views;

namespace NexusWorkspace.UI;

// Fully qualified: 'Application' alone binds to the NexusWorkspace.Application
// namespace (enclosing-namespace member) instead of Avalonia's Application type.
public partial class App : Avalonia.Application
{
    /// <summary>Set by the platform head before the Avalonia lifetime starts.</summary>
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (!DataTemplates.OfType<ViewLocator>().Any())
        {
            DataTemplates.Add(new ViewLocator());
        }

        var settings = Services.GetRequiredService<ISettingsStore>();
        Services.GetRequiredService<IThemeService>().Apply(settings.Current.Theme);

        var shell = Services.GetRequiredService<MainViewModel>();

        var hotkeys = Services.GetService<IGlobalHotkeyService>();
        var scheduler = Services.GetService<ISchedulerService>();
        var notifier = Services.GetService<InAppNotifier>();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new MainWindow { DataContext = shell };
                notifier?.Attach(window);
                window.Opened += (_, _) =>
                {
                    hotkeys?.Start();
                    scheduler?.Start();
                };
                desktop.ShutdownRequested += (_, _) =>
                {
                    hotkeys?.Stop();
                    scheduler?.Stop();
                };
                desktop.MainWindow = window;
                break;

            case ISingleViewApplicationLifetime singleView:
                var view = new MainView { DataContext = shell };
                singleView.MainView = view;
                view.AttachedToVisualTree += (_, _) =>
                {
                    if (TopLevel.GetTopLevel(view) is { } topLevel)
                    {
                        notifier?.Attach(topLevel);
                    }

                    scheduler?.Start();
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
