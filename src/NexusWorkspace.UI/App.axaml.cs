using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels;
using NexusWorkspace.UI.Views;

namespace NexusWorkspace.UI;

public partial class App : Application
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

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new MainWindow { DataContext = shell };
                var hotkeys = Services.GetService<IGlobalHotkeyService>();
                if (hotkeys is not null)
                {
                    window.Opened += (_, _) => hotkeys.Start();
                    desktop.ShutdownRequested += (_, _) => hotkeys.Stop();
                }

                desktop.MainWindow = window;
                break;

            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView { DataContext = shell };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
