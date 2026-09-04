using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Communications;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.FollowUps;
using NexusWorkspace.Domain.Inbox;
using NexusWorkspace.Domain.Meetings;
using NexusWorkspace.Domain.Notifications;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Relations;
using NexusWorkspace.Domain.Reminders;
using NexusWorkspace.Domain.SavedSearches;
using NexusWorkspace.Domain.Tags;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Infrastructure.Persistence;

/// <summary>The single SQLite context. Implements <see cref="IApplicationDbContext"/>.</summary>
public sealed class NexusDbContext(DbContextOptions<NexusDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();

    public DbSet<SubTask> SubTasks => Set<SubTask>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();

    public DbSet<WorkTaskTag> WorkTaskTags => Set<WorkTaskTag>();

    public DbSet<PersonTag> PersonTags => Set<PersonTag>();

    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();

    public DbSet<ProjectPerson> ProjectPeople => Set<ProjectPerson>();

    public DbSet<ProjectCompany> ProjectCompanies => Set<ProjectCompany>();

    public DbSet<WorkTaskPerson> WorkTaskPeople => Set<WorkTaskPerson>();

    public DbSet<Communication> Communications => Set<Communication>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();

    public DbSet<EntityRelation> EntityRelations => Set<EntityRelation>();

    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    public DbSet<InboxItem> InboxItems => Set<InboxItem>();

    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store every enum as text: readable in the DB and stable across reorders.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);

        // Store GUIDs as 36-char lowercase text: readable, consistent with the raw
        // SQL used by the FTS5 search index, and still time-sortable (v7).
        configurationBuilder.Properties<Guid>().HaveConversion<GuidToStringConverter>().HaveMaxLength(36);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusDbContext).Assembly);

        // Global soft-delete filter on every ISoftDelete entity.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Not(isDeleted);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(notDeleted, parameter));
        }

        base.OnModelCreating(modelBuilder);
    }
}
