using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Desktop.Platform;
using NexusWorkspace.Infrastructure;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.UI;
using Serilog;
using Velopack;

namespace NexusWorkspace.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Must be the very first thing that runs: Velopack briefly re-launches the
        // exe with hook arguments during install / update / uninstall and expects a
        // fast exit. It is a no-op for a normal launch or a non-installed build.
        VelopackApp.Build().Run();

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

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error(e.ExceptionObject as Exception, "Excepción no controlada en el dominio de la aplicación.");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Excepción de tarea no observada.");
            e.SetObserved();
        };

        try
        {
            Log.Information(
                "NexusWorkspace iniciando. Datos en {Root} (portable: {Portable}).",
                paths.RootDirectory,
                paths.IsPortable);

            // A staged restore / import must run before EF opens the database file.
            provider.GetRequiredService<IBackupService>().ApplyStagedAsync().GetAwaiter().GetResult();

            using (var scope = provider.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync()
                    .GetAwaiter()
                    .GetResult();
            }

            RunScheduledBackup(provider);

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

    /// <summary>Daily automatic backup: at most one per ~20 h when the schedule is not "None".</summary>
    private static void RunScheduledBackup(IServiceProvider provider)
    {
        try
        {
            var settings = provider.GetRequiredService<ISettingsStore>().Current.Backup;
            if (string.Equals(settings.Schedule, "None", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var backup = provider.GetRequiredService<IBackupService>();
            var existing = backup.ListAsync().GetAwaiter().GetResult();

            var interval = string.Equals(settings.Schedule, "Weekly", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromDays(7)
                : TimeSpan.FromHours(20);

            var last = existing
                .Where(b => b.Reason is "daily" or "weekly" or "manual")
                .Select(b => b.CreatedAtUtc)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (DateTime.UtcNow - last >= interval)
            {
                var reason = string.Equals(settings.Schedule, "Weekly", StringComparison.OrdinalIgnoreCase) ? "weekly" : "daily";
                backup.CreateAsync(reason).GetAwaiter().GetResult();
            }

            backup.PruneAsync(Math.Max(1, settings.KeepVersions)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo ejecutar la copia de seguridad programada.");
        }
    }

    // Referenced by the Avalonia previewer / designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
