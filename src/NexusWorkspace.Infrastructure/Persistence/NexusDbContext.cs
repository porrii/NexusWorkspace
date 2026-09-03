using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Inbox;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;
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

    public DbSet<ProjectPerson> ProjectPeople => Set<ProjectPerson>();

    public DbSet<ProjectCompany> ProjectCompanies => Set<ProjectCompany>();

    public DbSet<WorkTaskPerson> WorkTaskPeople => Set<WorkTaskPerson>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    public DbSet<InboxItem> InboxItems => Set<InboxItem>();

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
