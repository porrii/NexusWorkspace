using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.QuickActions;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Application.Tasks;

/// <summary>Write-side operations for tasks, subtasks, checklist, comments and quick actions.</summary>
public sealed class WorkTaskService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreateWorkTaskRequest request, CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Guid>("task.title_required", "El título de la tarea es obligatorio.");
        }

        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Result.Failure<Guid>("task.project_not_found", "El proyecto indicado no existe.");
        }

        var maxSortKey = await db.WorkTasks
            .Where(t => t.ProjectId == request.ProjectId)
            .Select(t => (double?)t.SortKey)
            .MaxAsync(cancellationToken) ?? 0d;

        var task = new WorkTask
        {
            ProjectId = request.ProjectId,
            Title = title,
            Description = Clean(request.Description),
            Priority = request.Priority,
            Status = WorkTaskStatus.Pending,
            DueDateUtc = request.DueDateUtc,
            AssigneePersonId = request.AssigneePersonId,
            RelatedCompanyId = request.RelatedCompanyId,
            SortKey = maxSortKey + 1d,
        };

        db.WorkTasks.Add(task);
        activity.Record(EntityKind.WorkTask, task.Id, ActivityType.Created,
            $"Tarea «{task.Title}» creada.", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return task.Id;
    }

    public async Task<Result> UpdateDetailsAsync(UpdateWorkTaskDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (request.Title is { } t && !string.IsNullOrWhiteSpace(t) && t.Trim() != task.Title)
        {
            var old = task.Title;
            task.Title = t.Trim();
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.Renamed,
                $"Tarea renombrada: «{old}» → «{task.Title}».", task.ProjectId, old, task.Title);
        }

        if (request.Description is not null)
        {
            task.Description = Clean(request.Description);
        }

        if (request.Priority is { } priority && priority != task.Priority)
        {
            var old = task.Priority;
            task.Priority = priority;
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.PriorityChanged,
                $"Prioridad: {DisplayNames.Of(old)} → {DisplayNames.Of(priority)}.",
                task.ProjectId, old.ToString(), priority.ToString());
        }

        if (request.ClearDueDate)
        {
            task.DueDateUtc = null;
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.DueDateChanged,
                "Fecha límite eliminada.", task.ProjectId);
        }
        else if (request.DueDateUtc is { } due && due != task.DueDateUtc)
        {
            task.DueDateUtc = due;
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.DueDateChanged,
                $"Fecha límite: {due:dd/MM/yyyy}.", task.ProjectId, newValue: due.ToString("O"));
        }

        if (request.ChangeAssignee && request.AssigneePersonId != task.AssigneePersonId)
        {
            task.AssigneePersonId = request.AssigneePersonId;
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.AssigneeChanged,
                "Responsable actualizado.", task.ProjectId);
        }

        if (request.ChangeRelatedCompany && request.RelatedCompanyId != task.RelatedCompanyId)
        {
            task.RelatedCompanyId = request.RelatedCompanyId;
            activity.Record(EntityKind.WorkTask, task.Id, ActivityType.Updated,
                "Empresa relacionada actualizada.", task.ProjectId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(Guid taskId, WorkTaskStatus target, CancellationToken cancellationToken = default)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var result = ApplyStatus(task, target);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<Guid>> AddSubTaskAsync(Guid taskId, string title, Guid? parentSubTaskId = null, CancellationToken cancellationToken = default)
    {
        title = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Guid>("subtask.title_required", "El título de la subtarea es obligatorio.");
        }

        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Guid>("task.not_found", "Tarea no encontrada.");
        }

        var maxSortKey = await db.SubTasks
            .Where(s => s.WorkTaskId == taskId && s.ParentSubTaskId == parentSubTaskId)
            .Select(s => (double?)s.SortKey)
            .MaxAsync(cancellationToken) ?? 0d;

        var subTask = new SubTask
        {
            WorkTaskId = taskId,
            ParentSubTaskId = parentSubTaskId,
            Title = title,
            SortKey = maxSortKey + 1d,
        };

        db.SubTasks.Add(subTask);
        activity.Record(EntityKind.WorkTask, taskId, ActivityType.SubTaskAdded,
            $"Subtarea añadida: «{title}».", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return subTask.Id;
    }

    public async Task<Result> SetSubTaskDoneAsync(Guid subTaskId, bool done, CancellationToken cancellationToken = default)
    {
        var subTask = await db.SubTasks.Include(s => s.WorkTask)
            .FirstOrDefaultAsync(s => s.Id == subTaskId, cancellationToken);
        if (subTask is null)
        {
            return Result.Failure("subtask.not_found", "Subtarea no encontrada.");
        }

        if (subTask.IsDone == done)
        {
            return Result.Success();
        }

        subTask.IsDone = done;
        subTask.CompletedAtUtc = done ? clock.UtcNow : null;

        if (done)
        {
            activity.Record(EntityKind.WorkTask, subTask.WorkTaskId, ActivityType.SubTaskCompleted,
                $"Subtarea completada: «{subTask.Title}».", subTask.WorkTask.ProjectId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<Guid>> AddChecklistItemAsync(Guid taskId, string text, CancellationToken cancellationToken = default)
    {
        text = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<Guid>("checklist.text_required", "El texto del ítem es obligatorio.");
        }

        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Guid>("task.not_found", "Tarea no encontrada.");
        }

        var maxSortKey = await db.ChecklistItems
            .Where(c => c.WorkTaskId == taskId)
            .Select(c => (double?)c.SortKey)
            .MaxAsync(cancellationToken) ?? 0d;

        var item = new ChecklistItem
        {
            WorkTaskId = taskId,
            Text = text,
            SortKey = maxSortKey + 1d,
        };

        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<Result> SetChecklistItemCheckedAsync(Guid itemId, bool isChecked, CancellationToken cancellationToken = default)
    {
        var item = await db.ChecklistItems.Include(c => c.WorkTask)
            .FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure("checklist.not_found", "Ítem no encontrado.");
        }

        if (item.IsChecked == isChecked)
        {
            return Result.Success();
        }

        item.IsChecked = isChecked;
        item.CheckedAtUtc = isChecked ? clock.UtcNow : null;
        activity.Record(EntityKind.WorkTask, item.WorkTaskId, ActivityType.ChecklistItemToggled,
            $"Checklist: «{item.Text}» {(isChecked ? "marcado" : "desmarcado")}.", item.WorkTask.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RenameSubTaskAsync(Guid subTaskId, string title, CancellationToken cancellationToken = default)
    {
        title = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure("subtask.title_required", "El título de la subtarea es obligatorio.");
        }

        var subTask = await db.SubTasks.FirstOrDefaultAsync(s => s.Id == subTaskId, cancellationToken);
        if (subTask is null)
        {
            return Result.Failure("subtask.not_found", "Subtarea no encontrada.");
        }

        subTask.Title = title;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteSubTaskAsync(Guid subTaskId, CancellationToken cancellationToken = default)
    {
        var subTask = await db.SubTasks.Include(s => s.WorkTask)
            .FirstOrDefaultAsync(s => s.Id == subTaskId, cancellationToken);
        if (subTask is null)
        {
            return Result.Success();
        }

        // Soft-delete the node and every descendant.
        var all = await db.SubTasks.Where(s => s.WorkTaskId == subTask.WorkTaskId && !s.IsDeleted)
            .ToListAsync(cancellationToken);
        var doomed = new HashSet<Guid> { subTaskId };
        bool grew;
        do
        {
            grew = false;
            foreach (var s in all)
            {
                if (!doomed.Contains(s.Id) && s.ParentSubTaskId is { } p && doomed.Contains(p))
                {
                    doomed.Add(s.Id);
                    grew = true;
                }
            }
        }
        while (grew);

        foreach (var s in all.Where(s => doomed.Contains(s.Id)))
        {
            s.IsDeleted = true;
            s.DeletedAtUtc = clock.UtcNow;
        }

        activity.Record(EntityKind.WorkTask, subTask.WorkTaskId, ActivityType.Updated,
            $"Subtarea eliminada: «{subTask.Title}».", subTask.WorkTask.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MoveSubTaskAsync(Guid subTaskId, bool up, CancellationToken cancellationToken = default)
    {
        var subTask = await db.SubTasks.FirstOrDefaultAsync(s => s.Id == subTaskId, cancellationToken);
        if (subTask is null)
        {
            return Result.Failure("subtask.not_found", "Subtarea no encontrada.");
        }

        var siblings = await db.SubTasks
            .Where(s => s.WorkTaskId == subTask.WorkTaskId && s.ParentSubTaskId == subTask.ParentSubTaskId && !s.IsDeleted)
            .OrderBy(s => s.SortKey)
            .ToListAsync(cancellationToken);

        var index = siblings.FindIndex(s => s.Id == subTaskId);
        var target = up ? index - 1 : index + 1;
        if (index < 0 || target < 0 || target >= siblings.Count)
        {
            return Result.Success();
        }

        (siblings[index].SortKey, siblings[target].SortKey) = (siblings[target].SortKey, siblings[index].SortKey);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RenameChecklistItemAsync(Guid itemId, string text, CancellationToken cancellationToken = default)
    {
        text = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure("checklist.text_required", "El texto del ítem es obligatorio.");
        }

        var item = await db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure("checklist.not_found", "Ítem no encontrado.");
        }

        item.Text = text;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteChecklistItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result.Success();
        }

        item.IsDeleted = true;
        item.DeletedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MoveChecklistItemAsync(Guid itemId, bool up, CancellationToken cancellationToken = default)
    {
        var item = await db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure("checklist.not_found", "Ítem no encontrado.");
        }

        var siblings = await db.ChecklistItems
            .Where(c => c.WorkTaskId == item.WorkTaskId && !c.IsDeleted)
            .OrderBy(c => c.SortKey)
            .ToListAsync(cancellationToken);

        var index = siblings.FindIndex(c => c.Id == itemId);
        var target = up ? index - 1 : index + 1;
        if (index < 0 || target < 0 || target >= siblings.Count)
        {
            return Result.Success();
        }

        (siblings[index].SortKey, siblings[target].SortKey) = (siblings[target].SortKey, siblings[index].SortKey);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<Guid>> AddCommentAsync(Guid taskId, string body, CancellationToken cancellationToken = default)
    {
        body = body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure<Guid>("comment.body_required", "El comentario no puede estar vacío.");
        }

        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Guid>("task.not_found", "Tarea no encontrada.");
        }

        var comment = new Comment
        {
            TargetKind = EntityKind.WorkTask,
            TargetId = taskId,
            ProjectId = task.ProjectId,
            Body = body,
        };

        db.Comments.Add(comment);
        activity.Record(EntityKind.WorkTask, taskId, ActivityType.CommentAdded,
            "Comentario añadido.", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }

    public async Task<Result<Guid>> AddDependencyAsync(AddDependencyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.WorkTaskId == request.DependsOnWorkTaskId)
        {
            return Result.Failure<Guid>("dependency.self", "Una tarea no puede depender de sí misma.");
        }

        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.WorkTaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Guid>("task.not_found", "Tarea no encontrada.");
        }

        var other = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.DependsOnWorkTaskId, cancellationToken);
        if (other is null)
        {
            return Result.Failure<Guid>("dependency.target_not_found", "La tarea de la que se quiere depender no existe.");
        }

        var alreadyLinked = await db.TaskDependencies.AnyAsync(
            d => d.WorkTaskId == request.WorkTaskId && d.DependsOnWorkTaskId == request.DependsOnWorkTaskId,
            cancellationToken);
        if (alreadyLinked)
        {
            return Result.Failure<Guid>("dependency.exists", "Esa dependencia ya existe.");
        }

        var reverseExists = await db.TaskDependencies.AnyAsync(
            d => d.WorkTaskId == request.DependsOnWorkTaskId && d.DependsOnWorkTaskId == request.WorkTaskId,
            cancellationToken);
        if (reverseExists)
        {
            return Result.Failure<Guid>("dependency.cycle", "Esa dependencia crearía un ciclo directo.");
        }

        var dependency = new TaskDependency
        {
            WorkTaskId = request.WorkTaskId,
            DependsOnWorkTaskId = request.DependsOnWorkTaskId,
            Kind = request.Kind,
        };

        db.TaskDependencies.Add(dependency);
        activity.Record(EntityKind.WorkTask, request.WorkTaskId, ActivityType.DependencyAdded,
            $"Depende de «{other.Title}».", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return dependency.Id;
    }

    public async Task<Result> RemoveDependencyAsync(Guid dependencyId, CancellationToken cancellationToken = default)
    {
        var dependency = await db.TaskDependencies
            .Include(d => d.WorkTask)
            .Include(d => d.DependsOnWorkTask)
            .FirstOrDefaultAsync(d => d.Id == dependencyId, cancellationToken);
        if (dependency is null)
        {
            return Result.Success();
        }

        db.TaskDependencies.Remove(dependency);
        activity.Record(EntityKind.WorkTask, dependency.WorkTaskId, ActivityType.DependencyRemoved,
            $"Ya no depende de «{dependency.DependsOnWorkTask.Title}».", dependency.WorkTask.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ExecuteQuickActionAsync(Guid taskId, QuickActionKind kind, string? note = null, CancellationToken cancellationToken = default)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var descriptor = QuickActionCatalog.Get(kind);
        activity.Record(EntityKind.WorkTask, task.Id, ActivityType.QuickAction,
            descriptor.Label, task.ProjectId, note: note, quickAction: kind);

        if (descriptor.PushesStatus is { } pushed && WorkTaskStateMachine.CanTransition(task.Status, pushed))
        {
            ApplyStatus(task, pushed);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AddTagAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return Result.Failure("tag.not_found", "Etiqueta no encontrada.");
        }

        var exists = await db.WorkTaskTags.AnyAsync(x => x.WorkTaskId == taskId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Success();
        }

        db.WorkTaskTags.Add(new WorkTaskTag { WorkTaskId = taskId, TagId = tagId });
        activity.Record(EntityKind.WorkTask, taskId, ActivityType.TagAdded,
            $"Etiqueta «{tag.Name}» añadida.", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var link = await db.WorkTaskTags.FirstOrDefaultAsync(x => x.WorkTaskId == taskId && x.TagId == tagId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        var projectId = await db.WorkTasks.Where(t => t.Id == taskId).Select(t => t.ProjectId).FirstOrDefaultAsync(cancellationToken);
        db.WorkTaskTags.Remove(link);
        activity.Record(EntityKind.WorkTask, taskId, ActivityType.TagRemoved, "Etiqueta retirada.", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(taskId, archived: true, cancellationToken);

    public Task<Result> UnarchiveAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(taskId, archived: false, cancellationToken);

    public Task<Result> TrashAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(taskId, trashed: true, cancellationToken);

    public Task<Result> RestoreFromTrashAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(taskId, trashed: false, cancellationToken);

    private Result ApplyStatus(WorkTask task, WorkTaskStatus target)
    {
        if (task.Status == target)
        {
            return Result.Success();
        }

        if (!WorkTaskStateMachine.CanTransition(task.Status, target))
        {
            return Result.Failure("task.invalid_transition",
                $"No se puede pasar de «{DisplayNames.Of(task.Status)}» a «{DisplayNames.Of(target)}».");
        }

        var old = task.Status;
        task.Status = target;

        if (task.StartedAtUtc is null && target is WorkTaskStatus.InProgress)
        {
            task.StartedAtUtc = clock.UtcNow;
        }

        task.CompletedDateUtc = target == WorkTaskStatus.Finished ? clock.UtcNow
            : old == WorkTaskStatus.Finished ? null
            : task.CompletedDateUtc;

        activity.Record(EntityKind.WorkTask, task.Id, ActivityType.StatusChanged,
            $"Estado: {DisplayNames.Of(old)} → {DisplayNames.Of(target)}.",
            task.ProjectId, old.ToString(), target.ToString());

        return Result.Success();
    }

    private async Task<Result> SetArchivedAsync(Guid taskId, bool archived, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (task.IsArchived == archived)
        {
            return Result.Success();
        }

        task.IsArchived = archived;
        activity.Record(EntityKind.WorkTask, task.Id,
            archived ? ActivityType.Archived : ActivityType.Restored,
            archived ? $"Tarea «{task.Title}» archivada." : $"Tarea «{task.Title}» restaurada.",
            task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetTrashedAsync(Guid taskId, bool trashed, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (task.IsDeleted == trashed)
        {
            return Result.Success();
        }

        task.IsDeleted = trashed;
        activity.Record(EntityKind.WorkTask, task.Id,
            trashed ? ActivityType.Trashed : ActivityType.RestoredFromTrash,
            trashed ? $"Tarea «{task.Title}» enviada a la papelera." : $"Tarea «{task.Title}» recuperada.",
            task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("task.not_found", "Tarea no encontrada.");
}
