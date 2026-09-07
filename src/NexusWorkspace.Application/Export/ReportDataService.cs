using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;

namespace NexusWorkspace.Application.Export;

/// <summary>Turns read-model data into the format-agnostic report payloads.</summary>
public sealed class ReportDataService(
    ProjectReadService projectReads,
    WorkTaskReadService taskReads,
    FollowUpReadService followUpReads,
    CommunicationReadService communicationReads,
    ActivityReadService activityReads)
{
    public static TableExport ProjectsTable(IReadOnlyList<ProjectListItem> projects) => new()
    {
        BaseName = "proyectos",
        Headers = ["Nombre", "Estado", "Prioridad", "Abiertas", "Finalizadas", "Progreso %", "Vencimiento"],
        Rows = projects.Select(p => (IReadOnlyList<string?>)new string?[]
        {
            p.Name,
            DisplayNames.Of(p.Status),
            DisplayNames.Of(p.Priority),
            p.OpenTaskCount.ToString(),
            p.FinishedTaskCount.ToString(),
            p.ProgressPercent.ToString(),
            p.DueDateUtc?.ToString("yyyy-MM-dd"),
        }).ToList(),
    };

    public static TableExport TasksTable(string baseName, IReadOnlyList<WorkTaskListItem> tasks) => new()
    {
        BaseName = baseName,
        Headers = ["Título", "Proyecto", "Estado", "Prioridad", "Vencimiento", "Responsable", "Subtareas %"],
        Rows = tasks.Select(t => (IReadOnlyList<string?>)new string?[]
        {
            t.Title,
            t.ProjectName,
            DisplayNames.Of(t.Status),
            DisplayNames.Of(t.Priority),
            t.DueDateUtc?.ToString("yyyy-MM-dd"),
            t.AssigneeName,
            t.SubTaskProgressPercent.ToString(),
        }).ToList(),
    };

    public async Task<ProjectReportData?> BuildProjectReportAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var header = await projectReads.GetDetailAsync(projectId, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var tasks = await taskReads.GetForProjectAsync(projectId, TaskListScope.All, cancellationToken);
        var followUps = await followUpReads.GetForEntityAsync(Domain.Enums.EntityKind.Project, projectId, cancellationToken);
        var comms = await communicationReads.GetForProjectAsync(projectId, 500, cancellationToken);
        var timeline = await activityReads.GetForProjectAsync(projectId, 500, cancellationToken);

        return new ProjectReportData
        {
            ProjectName = header.Name,
            Description = header.Description,
            Status = DisplayNames.Of(header.Status),
            Priority = DisplayNames.Of(header.Priority),
            StartDateUtc = header.StartDateUtc,
            DueDateUtc = header.DueDateUtc,
            ProgressPercent = header.ProgressPercent,
            GeneratedAtUtc = DateTime.UtcNow,
            Tasks = tasks.Select(t => new ReportTaskRow(
                t.Title,
                DisplayNames.Of(t.Status),
                DisplayNames.Of(t.Priority),
                t.DueDateUtc?.ToString("yyyy-MM-dd"),
                t.AssigneeName)).ToList(),
            FollowUps = followUps
                .Select(f => $"{f.Subject} — {f.WaitingOn} — {f.DaysWaiting} d ({DisplayNames.Of(f.State)})")
                .ToList(),
            Communications = comms
                .Select(c => $"{c.OccurredAtUtc:yyyy-MM-dd} · {DisplayNames.Of(c.Channel)}/{DisplayNames.Of(c.Direction)} · {c.Subject} · {c.With}")
                .ToList(),
            Timeline = timeline
                .Select(a => $"{a.OccurredAtUtc:yyyy-MM-dd HH:mm} · {a.Summary}")
                .ToList(),
        };
    }
}
