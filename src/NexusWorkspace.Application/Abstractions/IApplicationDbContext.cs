using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Collaboration;
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
using NexusWorkspace.Domain.Templates;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// The write/read surface the application layer uses. Implemented by
/// <c>NexusDbContext</c> in Infrastructure. Acts as the unit of work:
/// call <see cref="SaveChangesAsync"/> once per operation.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }

    DbSet<WorkTask> WorkTasks { get; }

    DbSet<SubTask> SubTasks { get; }

    DbSet<ChecklistItem> ChecklistItems { get; }

    DbSet<TaskDependency> TaskDependencies { get; }

    DbSet<Person> People { get; }

    DbSet<Company> Companies { get; }

    DbSet<Tag> Tags { get; }

    DbSet<ProjectTag> ProjectTags { get; }

    DbSet<WorkTaskTag> WorkTaskTags { get; }

    DbSet<PersonTag> PersonTags { get; }

    DbSet<CompanyTag> CompanyTags { get; }

    DbSet<ProjectPerson> ProjectPeople { get; }

    DbSet<ProjectCompany> ProjectCompanies { get; }

    DbSet<WorkTaskPerson> WorkTaskPeople { get; }

    DbSet<Communication> Communications { get; }

    DbSet<Meeting> Meetings { get; }

    DbSet<MeetingParticipant> MeetingParticipants { get; }

    DbSet<EntityRelation> EntityRelations { get; }

    DbSet<SavedSearch> SavedSearches { get; }

    DbSet<Template> Templates { get; }

    DbSet<Comment> Comments { get; }

    DbSet<Attachment> Attachments { get; }

    DbSet<ActivityEvent> ActivityEvents { get; }

    DbSet<InboxItem> InboxItems { get; }

    DbSet<FollowUp> FollowUps { get; }

    DbSet<Reminder> Reminders { get; }

    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
