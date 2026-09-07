using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Statistics;

/// <summary>Aggregates the workspace into a single <see cref="WorkspaceStats"/> snapshot.</summary>
public sealed class StatsReadService(IApplicationDbContext db, IClock clock)
{
    public async Task<WorkspaceStats> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var since30 = now.AddDays(-30);

        var projects = await db.Projects.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var tasks = await db.WorkTasks.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var taskPriorities = await db.WorkTasks.AsNoTracking()
            .GroupBy(t => t.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var overdue = await db.WorkTasks.AsNoTracking()
            .CountAsync(t => t.DueDateUtc != null && t.DueDateUtc < now
                             && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled, cancellationToken);

        var finishedTimes = await db.WorkTasks.AsNoTracking()
            .Where(t => t.Status == WorkTaskStatus.Finished && t.CompletedDateUtc != null)
            .Select(t => new { t.CreatedAtUtc, Done = t.CompletedDateUtc!.Value })
            .ToListAsync(cancellationToken);

        var avgDays = finishedTimes.Count == 0
            ? 0d
            : finishedTimes.Average(x => Math.Max(0, (x.Done - x.CreatedAtUtc).TotalDays));

        var finishedRecent = finishedTimes
            .Where(x => x.Done >= now.AddDays(-7 * 12))
            .Select(x => x.Done)
            .ToList();

        var openFollowUps = await db.FollowUps.AsNoTracking()
            .CountAsync(f => f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated, cancellationToken);

        var peopleCount = await db.People.AsNoTracking().CountAsync(cancellationToken);
        var companyCount = await db.Companies.AsNoTracking().CountAsync(cancellationToken);

        var attachmentCount = await db.Attachments.AsNoTracking().CountAsync(cancellationToken);
        var attachmentBytes = attachmentCount == 0
            ? 0L
            : await db.Attachments.AsNoTracking().SumAsync(a => a.SizeBytes, cancellationToken);

        var communications30 = await db.Communications.AsNoTracking()
            .CountAsync(c => c.OccurredAtUtc >= since30, cancellationToken);

        var incidents30 = await db.ActivityEvents.AsNoTracking()
            .CountAsync(e => e.OccurredAtUtc >= since30 && e.QuickAction == QuickActionKind.IncidentDetected, cancellationToken);

        var activityByType = await db.ActivityEvents.AsNoTracking()
            .Where(e => e.OccurredAtUtc >= since30)
            .GroupBy(e => e.Type)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var peopleLoad = await db.WorkTasks.AsNoTracking()
            .Where(t => t.AssigneePersonId != null
                        && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled)
            .GroupBy(t => t.Assignee!.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var totalTasks = tasks.Sum(t => t.Count);
        var finishedTasks = tasks.Where(t => t.Key == WorkTaskStatus.Finished).Sum(t => t.Count);
        var openTasks = tasks.Where(t => t.Key != WorkTaskStatus.Finished && t.Key != WorkTaskStatus.Cancelled).Sum(t => t.Count);

        return new WorkspaceStats
        {
            TotalProjectCount = projects.Sum(p => p.Count),
            ActiveProjectCount = projects.Where(p => p.Key is ProjectStatus.Active or ProjectStatus.Planning or ProjectStatus.OnHold or ProjectStatus.Blocked).Sum(p => p.Count),
            TotalTaskCount = totalTasks,
            OpenTaskCount = openTasks,
            FinishedTaskCount = finishedTasks,
            OverdueTaskCount = overdue,
            OpenFollowUpCount = openFollowUps,
            PeopleCount = peopleCount,
            CompanyCount = companyCount,
            AttachmentCount = attachmentCount,
            AttachmentBytes = attachmentBytes,
            CommunicationsLast30 = communications30,
            IncidentsLast30 = incidents30,
            AvgDaysToFinish = Math.Round(avgDays, 1),
            ProjectsByStatus = OrderedCounts(projects.Select(p => (DisplayNames.Of(p.Key), p.Count)), StatusOrder),
            TasksByStatus = OrderedCounts(tasks.Select(t => (DisplayNames.Of(t.Key), t.Count)), TaskStatusOrder),
            TasksByPriority = Enum.GetValues<Priority>()
                .Select(p => new NamedCount(DisplayNames.Of(p), taskPriorities.Where(x => x.Key == p).Sum(x => x.Count)))
                .ToList(),
            FinishedPerWeek = BuildWeeks(finishedRecent, today, 12),
            TopPeopleByOpenTasks = peopleLoad.Select(x => new NamedCount(x.Name, x.Count)).ToList(),
            ActivityByTypeLast30 = activityByType
                .OrderByDescending(x => x.Count)
                .Take(8)
                .Select(x => new NamedCount(x.Key.ToString(), x.Count))
                .ToList(),
        };
    }

    private static readonly string[] StatusOrder = ["Planificación", "Activo", "En pausa", "Bloqueado", "Finalizado"];

    private static readonly string[] TaskStatusOrder =
        ["Pendiente", "En progreso", "Esperando cliente", "Esperando proveedor", "Bloqueada", "Finalizada", "Cancelada"];

    private static IReadOnlyList<NamedCount> OrderedCounts(IEnumerable<(string Name, int Count)> source, string[] order)
    {
        var map = source.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        return order
            .Where(name => map.ContainsKey(name))
            .Select(name => new NamedCount(name, map[name]))
            .ToList();
    }

    private static IReadOnlyList<WeekPoint> BuildWeeks(IReadOnlyList<DateTime> completions, DateOnly today, int weeks)
    {
        var thisMonday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var points = new List<WeekPoint>();
        for (var i = weeks - 1; i >= 0; i--)
        {
            var start = thisMonday.AddDays(-7 * i);
            var end = start.AddDays(7);
            var count = completions.Count(c =>
            {
                var d = DateOnly.FromDateTime(c.ToLocalTime());
                return d >= start && d < end;
            });
            points.Add(new WeekPoint(start, count));
        }

        return points;
    }
}
