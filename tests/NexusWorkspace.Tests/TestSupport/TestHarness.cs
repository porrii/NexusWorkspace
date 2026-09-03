using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.Infrastructure.Persistence.Interceptors;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>
/// A real SQLite (in-memory) database plus wired application services, for
/// integration-style tests of the Phase 1 core.
/// </summary>
public sealed class TestHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public TestHarness()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Clock = new FixedClock(new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc));

        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditableEntityInterceptor(Clock))
            .Options;

        Db = new NexusDbContext(options);
        Db.Database.EnsureCreated();

        IActivityLog activityLog = new ActivityLog(Db, Clock);
        Projects = new ProjectService(Db, Clock, activityLog);
        Tasks = new WorkTaskService(Db, Clock, activityLog);
    }

    public FixedClock Clock { get; }

    public NexusDbContext Db { get; }

    public ProjectService Projects { get; }

    public WorkTaskService Tasks { get; }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
