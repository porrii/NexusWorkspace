using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Application.Projects;

/// <summary>Write-side operations for projects. Every meaningful change appends history.</summary>
public sealed class ProjectService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("project.name_required", "El nombre del proyecto es obligatorio.");
        }

        var project = new Project
        {
            Name = name,
            Description = Clean(request.Description),
            Icon = request.Icon,
            Color = request.Color,
            Priority = request.Priority,
            Status = ProjectStatus.Planning,
            StartDateUtc = request.StartDateUtc,
            DueDateUtc = request.DueDateUtc,
            OwnerPersonId = request.OwnerPersonId,
        };

        db.Projects.Add(project);
        activity.Record(EntityKind.Project, project.Id, ActivityType.Created,
            $"Proyecto «{project.Name}» creado.", project.Id);

        await db.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    public async Task<Result> UpdateDetailsAsync(UpdateProjectDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        if (request.Name is { } newName && !string.IsNullOrWhiteSpace(newName) && newName.Trim() != project.Name)
        {
            var old = project.Name;
            project.Name = newName.Trim();
            activity.Record(EntityKind.Project, project.Id, ActivityType.Renamed,
                $"Proyecto renombrado: «{old}» → «{project.Name}».", project.Id, old, project.Name);
        }

        if (request.Description is not null)
        {
            project.Description = Clean(request.Description);
        }

        if (request.Icon is not null)
        {
            project.Icon = request.Icon;
        }

        if (request.Color is not null)
        {
            project.Color = request.Color;
        }

        if (request.Priority is { } priority && priority != project.Priority)
        {
            var old = project.Priority;
            project.Priority = priority;
            activity.Record(EntityKind.Project, project.Id, ActivityType.PriorityChanged,
                $"Prioridad del proyecto: {DisplayNames.Of(old)} → {DisplayNames.Of(priority)}.",
                project.Id, old.ToString(), priority.ToString());
        }

        if (request.StartDateUtc.HasValue)
        {
            project.StartDateUtc = request.StartDateUtc;
        }

        if (request.DueDateUtc.HasValue)
        {
            project.DueDateUtc = request.DueDateUtc;
        }

        if (request.OwnerPersonId.HasValue)
        {
            project.OwnerPersonId = request.OwnerPersonId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(Guid projectId, ProjectStatus target, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        if (project.Status == target)
        {
            return Result.Success();
        }

        if (!ProjectStateMachine.CanTransition(project.Status, target))
        {
            return Result.Failure("project.invalid_transition",
                $"No se puede pasar de «{DisplayNames.Of(project.Status)}» a «{DisplayNames.Of(target)}».");
        }

        var old = project.Status;
        project.Status = target;
        project.CompletedDateUtc = target == ProjectStatus.Finished ? clock.UtcNow
            : old == ProjectStatus.Finished ? null
            : project.CompletedDateUtc;

        activity.Record(EntityKind.Project, project.Id, ActivityType.StatusChanged,
            $"Estado del proyecto: {DisplayNames.Of(old)} → {DisplayNames.Of(target)}.",
            project.Id, old.ToString(), target.ToString());

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid projectId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(projectId, archived: true, cancellationToken);

    public Task<Result> UnarchiveAsync(Guid projectId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(projectId, archived: false, cancellationToken);

    public Task<Result> TrashAsync(Guid projectId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(projectId, trashed: true, cancellationToken);

    public Task<Result> RestoreFromTrashAsync(Guid projectId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(projectId, trashed: false, cancellationToken);

    public async Task<Result> MarkOpenedAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        project.LastOpenedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetArchivedAsync(Guid projectId, bool archived, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        if (project.IsArchived == archived)
        {
            return Result.Success();
        }

        project.IsArchived = archived;
        activity.Record(EntityKind.Project, project.Id,
            archived ? ActivityType.Archived : ActivityType.Restored,
            archived ? $"Proyecto «{project.Name}» archivado." : $"Proyecto «{project.Name}» restaurado.",
            project.Id);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetTrashedAsync(Guid projectId, bool trashed, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        if (project.IsDeleted == trashed)
        {
            return Result.Success();
        }

        project.IsDeleted = trashed;
        activity.Record(EntityKind.Project, project.Id,
            trashed ? ActivityType.Trashed : ActivityType.RestoredFromTrash,
            trashed ? $"Proyecto «{project.Name}» enviado a la papelera." : $"Proyecto «{project.Name}» recuperado de la papelera.",
            project.Id);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("project.not_found", "Proyecto no encontrado.");
}
