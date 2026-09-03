using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Infrastructure.Seeding;

/// <summary>
/// Optional demo workspace so the UI can be judged with realistic content.
/// Fully reversible via <see cref="RemoveAsync"/>.
/// </summary>
public sealed class DemoDataSeeder(IApplicationDbContext db, IClock clock) : IDemoDataService
{
    public const string DemoProjectName = "Espacio de demostración";

    public async Task<bool> IsPresentAsync(CancellationToken cancellationToken = default)
        => await db.Projects.IgnoreQueryFilters().AnyAsync(p => p.Name == DemoProjectName, cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await IsPresentAsync(cancellationToken))
        {
            return;
        }

        var now = clock.UtcNow;

        var project = new Project
        {
            Name = DemoProjectName,
            Description = "Proyecto de ejemplo con tareas, subtareas, checklist, comentarios e historial. Puedes eliminarlo desde Configuración.",
            Icon = "RocketLaunch",
            Color = "#4A43D9",
            Status = ProjectStatus.Active,
            Priority = Priority.High,
            StartDateUtc = now.AddDays(-21),
            DueDateUtc = now.AddDays(14),
        };
        db.Projects.Add(project);

        AddEvent(project.Id, project.Id, ActivityType.Created, $"Proyecto «{project.Name}» creado.", now.AddDays(-21));

        var integrationTask = AddTask(project.Id, "Integración con SIP", WorkTaskStatus.WaitingProvider, Priority.High, now.AddDays(-14), now.AddDays(2));
        AddEvent(project.Id, integrationTask.Id, ActivityType.Created, "Tarea «Integración con SIP» creada.", now.AddDays(-14));
        AddEvent(project.Id, integrationTask.Id, ActivityType.PriorityChanged, "Prioridad: Media → Alta.", now.AddDays(-13), "Medium", "High");
        AddEvent(project.Id, integrationTask.Id, ActivityType.QuickAction, DisplayNames.Of(QuickActionKind.EmailSent), now.AddDays(-6), quickAction: QuickActionKind.EmailSent);
        AddEvent(project.Id, integrationTask.Id, ActivityType.StatusChanged, "Estado: En progreso → Esperando proveedor.", now.AddDays(-6), "InProgress", "WaitingProvider");

        db.SubTasks.AddRange(
            NewSubTask(integrationTask.Id, "Revisar API", true, 1),
            NewSubTask(integrationTask.Id, "Implementar llamada", true, 2),
            NewSubTask(integrationTask.Id, "Probar DESA", false, 3),
            NewSubTask(integrationTask.Id, "Probar PRE", false, 4),
            NewSubTask(integrationTask.Id, "Probar PRO", false, 5),
            NewSubTask(integrationTask.Id, "Documentar", false, 6));

        db.ChecklistItems.AddRange(
            NewCheck(integrationTask.Id, "DESA probado", true, 1),
            NewCheck(integrationTask.Id, "PRE probado", true, 2),
            NewCheck(integrationTask.Id, "PRO probado", false, 3),
            NewCheck(integrationTask.Id, "Documentación", false, 4));

        db.Comments.Add(new Comment
        {
            TargetKind = EntityKind.WorkTask,
            TargetId = integrationTask.Id,
            ProjectId = project.Id,
            Body = "Pendiente de que el proveedor confirme el endpoint de PRE. Enviado recordatorio.",
        });
        AddEvent(project.Id, integrationTask.Id, ActivityType.CommentAdded, "Comentario añadido.", now.AddDays(-5));

        var reportTask = AddTask(project.Id, "Método de informes cuatrimestrales", WorkTaskStatus.InProgress, Priority.Medium, now.AddDays(-9), now.AddDays(9));
        AddEvent(project.Id, reportTask.Id, ActivityType.Created, "Tarea «Método de informes cuatrimestrales» creada.", now.AddDays(-9));

        var videoTask = AddTask(project.Id, "Tamaño de vídeos en cartografía", WorkTaskStatus.Blocked, Priority.Critical, now.AddDays(-7), now.AddDays(-1));
        AddEvent(project.Id, videoTask.Id, ActivityType.Created, "Tarea «Tamaño de vídeos en cartografía» creada.", now.AddDays(-7));
        AddEvent(project.Id, videoTask.Id, ActivityType.StatusChanged, "Estado: Pendiente → Bloqueada.", now.AddDays(-4), "Pending", "Blocked");
        AddEvent(project.Id, videoTask.Id, ActivityType.QuickAction, DisplayNames.Of(QuickActionKind.IncidentDetected), now.AddDays(-4), quickAction: QuickActionKind.IncidentDetected);

        var deployTask = AddTask(project.Id, "Desplegar en PRO", WorkTaskStatus.Pending, Priority.High, now.AddDays(-3), now.AddDays(7));
        AddEvent(project.Id, deployTask.Id, ActivityType.Created, "Tarea «Desplegar en PRO» creada.", now.AddDays(-3));
        db.TaskDependencies.Add(new TaskDependency
        {
            WorkTaskId = deployTask.Id,
            DependsOnWorkTaskId = integrationTask.Id,
            Kind = DependencyKind.FinishToStart,
        });
        AddEvent(project.Id, deployTask.Id, ActivityType.DependencyAdded, "Depende de «Integración con SIP».", now.AddDays(-3));

        var doneTask = AddTask(project.Id, "Alta de máquina virtual", WorkTaskStatus.Finished, Priority.Medium, now.AddDays(-18), now.AddDays(-15));
        doneTask.CompletedDateUtc = now.AddDays(-15);
        AddEvent(project.Id, doneTask.Id, ActivityType.Created, "Tarea «Alta de máquina virtual» creada.", now.AddDays(-18));
        AddEvent(project.Id, doneTask.Id, ActivityType.StatusChanged, "Estado: En progreso → Finalizada.", now.AddDays(-15), "InProgress", "Finished");

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Name == DemoProjectName, cancellationToken);
        if (project is null)
        {
            return;
        }

        var events = await db.ActivityEvents.Where(a => a.ProjectId == project.Id).ToListAsync(cancellationToken);
        db.ActivityEvents.RemoveRange(events);

        var comments = await db.Comments.IgnoreQueryFilters()
            .Where(c => c.ProjectId == project.Id).ToListAsync(cancellationToken);
        db.Comments.RemoveRange(comments);

        // Tasks, subtasks, checklist items and dependencies cascade from the project.
        db.Projects.Remove(project);

        await db.SaveChangesAsync(cancellationToken);
    }

    private WorkTask AddTask(Guid projectId, string title, WorkTaskStatus status, Priority priority, DateTime startedAt, DateTime? due)
    {
        var task = new WorkTask
        {
            ProjectId = projectId,
            Title = title,
            Status = status,
            Priority = priority,
            StartedAtUtc = status is WorkTaskStatus.Pending ? null : startedAt,
            DueDateUtc = due,
            SortKey = db.WorkTasks.Local.Count(t => t.ProjectId == projectId) + 1,
        };
        db.WorkTasks.Add(task);
        return task;
    }

    private static SubTask NewSubTask(Guid taskId, string title, bool done, double sortKey) => new()
    {
        WorkTaskId = taskId,
        Title = title,
        IsDone = done,
        SortKey = sortKey,
    };

    private static ChecklistItem NewCheck(Guid taskId, string text, bool isChecked, double sortKey) => new()
    {
        WorkTaskId = taskId,
        Text = text,
        IsChecked = isChecked,
        SortKey = sortKey,
    };

    private void AddEvent(
        Guid projectId,
        Guid targetId,
        ActivityType type,
        string summary,
        DateTime occurredAtUtc,
        string? oldValue = null,
        string? newValue = null,
        QuickActionKind? quickAction = null)
    {
        db.ActivityEvents.Add(new ActivityEvent
        {
            TargetKind = targetId == projectId ? EntityKind.Project : EntityKind.WorkTask,
            TargetId = targetId,
            ProjectId = projectId,
            Type = type,
            Summary = summary,
            OldValue = oldValue,
            NewValue = newValue,
            QuickAction = quickAction,
            OccurredAtUtc = occurredAtUtc,
            ActorLabel = "yo",
        });
    }
}
