using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Desktop.Platform;
using NexusWorkspace.Infrastructure;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.UI;
using Serilog;

namespace NexusWorkspace.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddApplication();
        services.AddUi();
        services.AddSingleton<IPlatformLauncher, WindowsPlatformLauncher>();
        services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        var provider = services.BuildServiceProvider();
        var paths = provider.GetRequiredService<IAppPaths>();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "nexus-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                shared: true)
            .CreateLogger();

        try
        {
            Log.Information(
                "NexusWorkspace iniciando. Datos en {Root} (portable: {Portable}).",
                paths.RootDirectory,
                paths.IsPortable);

            using (var scope = provider.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync()
                    .GetAwaiter()
                    .GetResult();
            }

            App.Services = provider;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fallo no controlado al iniciar NexusWorkspace.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
            provider.Dispose();
        }
    }

    // Referenced by the Avalonia previewer / designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
