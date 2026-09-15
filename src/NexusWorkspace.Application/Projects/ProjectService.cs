using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Application.Projects;

/// <summary>Write-side operations for projects. Every meaningful change appends history.</summary>
public sealed class ProjectService(IApplicationDbContext db, IClock clock, IActivityLog activity, ISettingsStore settings)
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
            Status = settings.Current.DefaultProjectStatus,
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

        if (request.ClearStartDate)
        {
            project.StartDateUtc = null;
        }
        else if (request.StartDateUtc.HasValue)
        {
            project.StartDateUtc = request.StartDateUtc;
        }

        if (request.ClearDueDate)
        {
            project.DueDateUtc = null;
        }
        else if (request.DueDateUtc.HasValue)
        {
            project.DueDateUtc = request.DueDateUtc;
        }

        if (request.ChangeOwner)
        {
            project.OwnerPersonId = request.OwnerPersonId;
        }
        else if (request.OwnerPersonId.HasValue)
        {
            project.OwnerPersonId = request.OwnerPersonId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AddTagAsync(Guid projectId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return Result.Failure("tag.not_found", "Etiqueta no encontrada.");
        }

        var exists = await db.ProjectTags.AnyAsync(x => x.ProjectId == projectId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Success();
        }

        db.ProjectTags.Add(new ProjectTag { ProjectId = projectId, TagId = tagId });
        activity.Record(EntityKind.Project, projectId, ActivityType.TagAdded,
            $"Etiqueta «{tag.Name}» añadida.", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveTagAsync(Guid projectId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var link = await db.ProjectTags.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.TagId == tagId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        db.ProjectTags.Remove(link);
        activity.Record(EntityKind.Project, projectId, ActivityType.TagRemoved, "Etiqueta retirada.", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LinkPersonAsync(Guid projectId, Guid personId, string? role = null, CancellationToken cancellationToken = default)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            return NotFound();
        }

        var person = await db.People.FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return Result.Failure("person.not_found", "Persona no encontrada.");
        }

        if (await db.ProjectPeople.AnyAsync(x => x.ProjectId == projectId && x.PersonId == personId, cancellationToken))
        {
            return Result.Success();
        }

        db.ProjectPeople.Add(new ProjectPerson
        {
            ProjectId = projectId,
            PersonId = personId,
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
            LinkedAtUtc = clock.UtcNow,
        });
        activity.Record(EntityKind.Project, projectId, ActivityType.LinkedPerson,
            $"Persona vinculada: «{person.Name}».", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnlinkPersonAsync(Guid projectId, Guid personId, CancellationToken cancellationToken = default)
    {
        var link = await db.ProjectPeople.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.PersonId == personId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        db.ProjectPeople.Remove(link);
        activity.Record(EntityKind.Project, projectId, ActivityType.UnlinkedPerson, "Persona desvinculada.", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LinkCompanyAsync(Guid projectId, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            return NotFound();
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure("company.not_found", "Empresa no encontrada.");
        }

        if (await db.ProjectCompanies.AnyAsync(x => x.ProjectId == projectId && x.CompanyId == companyId, cancellationToken))
        {
            return Result.Success();
        }

        db.ProjectCompanies.Add(new ProjectCompany
        {
            ProjectId = projectId,
            CompanyId = companyId,
            LinkedAtUtc = clock.UtcNow,
        });
        activity.Record(EntityKind.Project, projectId, ActivityType.LinkedCompany,
            $"Empresa vinculada: «{company.Name}».", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnlinkCompanyAsync(Guid projectId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var link = await db.ProjectCompanies.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.CompanyId == companyId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        db.ProjectCompanies.Remove(link);
        activity.Record(EntityKind.Project, projectId, ActivityType.UnlinkedCompany, "Empresa desvinculada.", projectId);

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
