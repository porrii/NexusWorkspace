using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Attachments;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Meetings;
using NexusWorkspace.Application.Notifications;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Export;
using NexusWorkspace.Application.Import;
using NexusWorkspace.Application.SavedSearches;
using NexusWorkspace.Application.Statistics;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Application.Templates;
using NexusWorkspace.Application.Trash;
using NexusWorkspace.Infrastructure.Persistence;
using NexusWorkspace.Infrastructure.Persistence.Interceptors;
using NexusWorkspace.Infrastructure.Search;
using NexusWorkspace.Infrastructure.Storage;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>
/// A real SQLite (in-memory) database plus wired application services, for
/// integration-style tests of the Phase 1–3 core.
/// </summary>
public sealed class TestHarness : IAsyncDisposable, IDisposable
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

        People = new PersonService(Db, activityLog);
        PeopleReads = new PersonReadService(Db);
        Companies = new CompanyService(Db, activityLog);
        CompanyReads = new CompanyReadService(Db);
        Communications = new CommunicationService(Db, Clock, activityLog);
        CommunicationReads = new CommunicationReadService(Db);
        Meetings = new MeetingService(Db, activityLog);
        MeetingReads = new MeetingReadService(Db, Clock);
        Tags = new TagService(Db, activityLog);
        TagReads = new TagReadService(Db);
        Relations = new RelationService(Db, activityLog);
        RelationReads = new RelationReadService(Db);
        SavedSearches = new SavedSearchService(Db, Clock);
        SavedSearchReads = new SavedSearchReadService(Db);
        Activity = new ActivityReadService(Db);

        Paths = new TempPaths();
        AttachmentStore = new FileSystemAttachmentStore(Paths);
        Attachments = new AttachmentService(Db, AttachmentStore, activityLog);
        AttachmentReads = new AttachmentReadService(Db, AttachmentStore);

        ProjectReads = new ProjectReadService(Db);
        TaskReads = new WorkTaskReadService(Db);
        Stats = new StatsReadService(Db, Clock);
        Templates = new TemplateService(Db, Clock, activityLog);
        TemplateReads = new TemplateReadService(Db);
        Import = new ImportService(Db, activityLog);
        ReportData = new ReportDataService(ProjectReads, TaskReads,
            new FollowUpReadService(Db, Clock), CommunicationReads, Activity);
        Exporter = new NexusWorkspace.Infrastructure.Export.ReportExporter(Paths);
        Trash = new TrashService(Db, activityLog);
        TrashReads = new TrashReadService(Db);
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

    public PersonService People { get; }

    public PersonReadService PeopleReads { get; }

    public CompanyService Companies { get; }

    public CompanyReadService CompanyReads { get; }

    public CommunicationService Communications { get; }

    public CommunicationReadService CommunicationReads { get; }

    public MeetingService Meetings { get; }

    public MeetingReadService MeetingReads { get; }

    public TagService Tags { get; }

    public TagReadService TagReads { get; }

    public RelationService Relations { get; }

    public RelationReadService RelationReads { get; }

    public SavedSearchService SavedSearches { get; }

    public SavedSearchReadService SavedSearchReads { get; }

    public ActivityReadService Activity { get; }

    public TempPaths Paths { get; }

    public FileSystemAttachmentStore AttachmentStore { get; }

    public AttachmentService Attachments { get; }

    public AttachmentReadService AttachmentReads { get; }

    public ProjectReadService ProjectReads { get; }

    public WorkTaskReadService TaskReads { get; }

    public StatsReadService Stats { get; }

    public TemplateService Templates { get; }

    public TemplateReadService TemplateReads { get; }

    public ImportService Import { get; }

    public ReportDataService ReportData { get; }

    public NexusWorkspace.Infrastructure.Export.ReportExporter Exporter { get; }

    public TrashService Trash { get; }

    public TrashReadService TrashReads { get; }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
        Paths.Dispose();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        Paths.Dispose();
    }
}
