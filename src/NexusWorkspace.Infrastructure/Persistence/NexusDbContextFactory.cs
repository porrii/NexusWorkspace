using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexusWorkspace.Infrastructure.Storage;

namespace NexusWorkspace.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> works without booting the UI.
/// Uses the same on-disk location the app uses at runtime.
/// </summary>
public sealed class NexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
{
    public NexusDbContext CreateDbContext(string[] args)
    {
        var paths = new AppPaths();
        paths.EnsureCreated();

        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlite($"Data Source={paths.DatabasePath};Foreign Keys=True")
            .Options;

        return new NexusDbContext(options);
    }
}
