using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.Infrastructure.Persistence.Interceptors;
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

        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<NexusDbContext>((serviceProvider, options) =>
        {
            var paths = serviceProvider.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath};Foreign Keys=True");
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NexusDbContext>());

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IDemoDataService>(sp => sp.GetRequiredService<DemoDataSeeder>());

        return services;
    }
}
