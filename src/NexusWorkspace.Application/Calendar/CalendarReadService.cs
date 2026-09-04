using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Calendar;

public enum CalendarEntryKind
{
    TaskDue = 0,
    ProjectDue = 1,
    FollowUpNext = 2,
    Reminder = 3,
}

/// <summary>One dated item on the calendar. <see cref="Date"/> is the local calendar day.</summary>
public sealed record CalendarEntry
{
    public DateOnly Date { get; init; }

    public DateTime WhenUtc { get; init; }

    public CalendarEntryKind Kind { get; init; }

    public required string Title { get; init; }

    public EntityKind NavigateKind { get; init; }

    public Guid NavigateId { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }
}

public sealed class CalendarReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<CalendarEntry>> GetRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var tasks = await db.WorkTasks.AsNoTracking()
            .Where(t => t.DueDateUtc != null && t.DueDateUtc >= fromUtc && t.DueDateUtc <= toUtc
                        && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled)
            .Select(t => new { t.Id, t.Title, Due = t.DueDateUtc!.Value, t.ProjectId, ProjectName = t.Project.Name })
            .ToListAsync(cancellationToken);

        var projects = await db.Projects.AsNoTracking()
            .Where(p => p.DueDateUtc != null && p.DueDateUtc >= fromUtc && p.DueDateUtc <= toUtc
                        && p.Status != ProjectStatus.Finished)
            .Select(p => new { p.Id, p.Name, Due = p.DueDateUtc!.Value })
            .ToListAsync(cancellationToken);

        var followUps = await db.FollowUps.AsNoTracking()
            .Where(f => (f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated)
                        && f.NextFollowUpUtc != null && f.NextFollowUpUtc >= fromUtc && f.NextFollowUpUtc <= toUtc)
            .Select(f => new
            {
                f.Id,
                f.Subject,
                Next = f.NextFollowUpUtc!.Value,
                f.ProjectId,
                ProjectName = f.Project != null ? f.Project.Name : null,
            })
            .ToListAsync(cancellationToken);

        var reminders = await db.Reminders.AsNoTracking()
            .Where(r => r.Status == ReminderStatus.Pending && r.RemindAtUtc >= fromUtc && r.RemindAtUtc <= toUtc)
            .Select(r => new
            {
                r.Id,
                r.Text,
                At = r.RemindAtUtc,
                r.ProjectId,
                ProjectName = r.Project != null ? r.Project.Name : null,
            })
            .ToListAsync(cancellationToken);

        var entries = new List<CalendarEntry>(tasks.Count + projects.Count + followUps.Count + reminders.Count);

        entries.AddRange(tasks.Select(t => new CalendarEntry
        {
            Date = LocalDay(t.Due),
            WhenUtc = t.Due,
            Kind = CalendarEntryKind.TaskDue,
            Title = t.Title,
            NavigateKind = EntityKind.WorkTask,
            NavigateId = t.Id,
            ProjectId = t.ProjectId,
            ProjectName = t.ProjectName,
        }));

        entries.AddRange(projects.Select(p => new CalendarEntry
        {
            Date = LocalDay(p.Due),
            WhenUtc = p.Due,
            Kind = CalendarEntryKind.ProjectDue,
            Title = p.Name,
            NavigateKind = EntityKind.Project,
            NavigateId = p.Id,
            ProjectId = p.Id,
            ProjectName = p.Name,
        }));

        entries.AddRange(followUps.Select(f => new CalendarEntry
        {
            Date = LocalDay(f.Next),
            WhenUtc = f.Next,
            Kind = CalendarEntryKind.FollowUpNext,
            Title = f.Subject,
            NavigateKind = EntityKind.FollowUp,
            NavigateId = f.Id,
            ProjectId = f.ProjectId,
            ProjectName = f.ProjectName,
        }));

        entries.AddRange(reminders.Select(r => new CalendarEntry
        {
            Date = LocalDay(r.At),
            WhenUtc = r.At,
            Kind = CalendarEntryKind.Reminder,
            Title = r.Text,
            NavigateKind = EntityKind.Reminder,
            NavigateId = r.Id,
            ProjectId = r.ProjectId,
            ProjectName = r.ProjectName,
        }));

        return entries.OrderBy(e => e.WhenUtc).ToList();
    }

    private static DateOnly LocalDay(DateTime utc)
        => DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime());
}
