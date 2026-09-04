using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Notifications;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.Infrastructure.Persistence.Interceptors;
using NexusWorkspace.Infrastructure.Search;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>
/// A real SQLite (in-memory) database plus wired application services, for
/// integration-style tests of the Phase 1–3 core.
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
            .AddInterceptors(new AuditableEntityInterceptor(Clock), new SearchIndexInterceptor())
            .Options;

        Db = new NexusDbContext(options);
        Db.Database.EnsureCreated();
        Db.Database.ExecuteSqlRaw(Fts5SearchService.CreateTableSql);

        IActivityLog activityLog = new ActivityLog(Db, Clock);
        Projects = new ProjectService(Db, Clock, activityLog);
        Tasks = new WorkTaskService(Db, Clock, activityLog);
        Inbox = new InboxService(Db, Clock, activityLog);
        FollowUps = new FollowUpService(Db, Clock, activityLog);
        Reminders = new ReminderService(Db, Clock, activityLog);
        Notifications = new NotificationService(Db, Clock, new NullAppNotifier());
        Search = new Fts5SearchService(Db);
    }

    public FixedClock Clock { get; }

    public NexusDbContext Db { get; }

    public ProjectService Projects { get; }

    public WorkTaskService Tasks { get; }

    public InboxService Inbox { get; }

    public FollowUpService FollowUps { get; }

    public ReminderService Reminders { get; }

    public NotificationService Notifications { get; }

    public Fts5SearchService Search { get; }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
