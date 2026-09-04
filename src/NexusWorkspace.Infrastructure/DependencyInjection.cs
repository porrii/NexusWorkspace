using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.Infrastructure.Persistence.Interceptors;
using NexusWorkspace.Infrastructure.Scheduling;
using NexusWorkspace.Infrastructure.Search;
using NexusWorkspace.Infrastructure.Seeding;
using NexusWorkspace.Infrastructure.Settings;
using NexusWorkspace.Infrastructure.Storage;
using NexusWorkspace.Infrastructure.Time;

namespace NexusWorkspace.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure: paths, clock, settings, the SQLite context and its
    /// interceptors, and database bootstrap helpers.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? overrideRootDirectory = null)
    {
        services.AddSingleton<IAppPaths>(_ =>
        {
            var paths = new AppPaths(overrideRootDirectory);
            paths.EnsureCreated();
            return paths;
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IAttachmentStore, FileSystemAttachmentStore>();

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<SearchIndexInterceptor>();

        services.AddDbContext<NexusDbContext>((serviceProvider, options) =>
        {
            var paths = serviceProvider.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath};Foreign Keys=True");
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<SearchIndexInterceptor>());

            // Join entities (ProjectTag, WorkTaskPerson…) have no soft-delete filter
            // while their parents do. We only ever query from the filtered parent, so
            // this interaction is benign here.
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NexusDbContext>());

        services.AddScoped<Fts5SearchService>();
        services.AddScoped<ISearchService>(sp => sp.GetRequiredService<Fts5SearchService>());

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IDemoDataService>(sp => sp.GetRequiredService<DemoDataSeeder>());

        services.AddSingleton<ISchedulerService, InProcessScheduler>();

        return services;
    }
}
