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
    Meeting = 4,
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

        // Recurring reminders only store their next occurrence, so instead of filtering by
        // date in SQL, every pending reminder is fetched and its occurrences within the
        // visible range are expanded in memory (cheap: this is a small local table).
        var pendingReminders = await db.Reminders.AsNoTracking()
            .Where(r => r.Status == ReminderStatus.Pending)
            .Select(r => new
            {
                r.Id,
                r.Text,
                r.RemindAtUtc,
                r.RecurrenceFrequency,
                r.RecurrenceInterval,
                r.RecurrenceEndUtc,
                r.ProjectId,
                ProjectName = r.Project != null ? r.Project.Name : null,
            })
            .ToListAsync(cancellationToken);

        var reminders = pendingReminders
            .SelectMany(r => ExpandOccurrences(r.RemindAtUtc, r.RecurrenceFrequency, r.RecurrenceInterval, r.RecurrenceEndUtc, fromUtc, toUtc)
                .Select(at => new { r.Id, r.Text, At = at, r.ProjectId, r.ProjectName }))
            .ToList();

        var meetings = await db.Meetings.AsNoTracking()
            .Where(m => m.Status != MeetingStatus.Cancelled && m.StartUtc >= fromUtc && m.StartUtc <= toUtc)
            .Select(m => new
            {
                m.Id,
                m.Title,
                At = m.StartUtc,
                m.ProjectId,
                ProjectName = m.Project != null ? m.Project.Name : null,
            })
            .ToListAsync(cancellationToken);

        var entries = new List<CalendarEntry>(
            tasks.Count + projects.Count + reminders.Count + meetings.Count);

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

        entries.AddRange(meetings.Select(m => new CalendarEntry
        {
            Date = LocalDay(m.At),
            WhenUtc = m.At,
            Kind = CalendarEntryKind.Meeting,
            Title = m.Title,
            NavigateKind = EntityKind.Meeting,
            NavigateId = m.Id,
            ProjectId = m.ProjectId,
            ProjectName = m.ProjectName,
        }));

        return entries.OrderBy(e => e.WhenUtc).ToList();
    }

    private static DateOnly LocalDay(DateTime utc)
        => DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime());

    /// <summary>
    /// Every occurrence of a (possibly recurring) reminder that falls within [fromUtc, toUtc].
    /// <paramref name="anchorUtc"/> is the reminder's current RemindAtUtc, which may sit before or
    /// after the requested range, so it is walked in both directions rather than assumed to be first.
    /// </summary>
    private static IEnumerable<DateTime> ExpandOccurrences(
        DateTime anchorUtc, RecurrenceFrequency frequency, int interval, DateTime? recurrenceEndUtc,
        DateTime fromUtc, DateTime toUtc)
    {
        if (frequency == RecurrenceFrequency.None)
        {
            if (anchorUtc >= fromUtc && anchorUtc <= toUtc)
            {
                yield return anchorUtc;
            }

            yield break;
        }

        interval = Math.Max(1, interval);
        var rangeEnd = recurrenceEndUtc is { } end && end < toUtc ? end : toUtc;

        DateTime Step(DateTime d, int direction) => frequency switch
        {
            RecurrenceFrequency.Daily => d.AddDays(interval * direction),
            RecurrenceFrequency.Weekly => d.AddDays(7 * interval * direction),
            RecurrenceFrequency.Monthly => d.AddMonths(interval * direction),
            _ => d,
        };

        const int guardLimit = 2000;

        var current = anchorUtc;
        for (var i = 0; current > fromUtc && i < guardLimit; i++)
        {
            current = Step(current, -1);
        }

        for (var i = 0; current <= rangeEnd && i < guardLimit; i++)
        {
            if (current >= fromUtc)
            {
                yield return current;
            }

            current = Step(current, 1);
        }
    }
}
