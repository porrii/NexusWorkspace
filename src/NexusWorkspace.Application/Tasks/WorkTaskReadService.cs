using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Tasks;

/// <summary>Read-side queries for tasks. Always projected and no-tracking.</summary>
public sealed class WorkTaskReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<WorkTaskListItem>> GetForProjectAsync(
        Guid projectId,
        TaskListScope scope = TaskListScope.Open,
        CancellationToken cancellationToken = default)
    {
        var query = db.WorkTasks.AsNoTracking().IgnoreQueryFilters()
            .Where(t => t.ProjectId == projectId && !t.IsDeleted);

        query = scope switch
        {
            TaskListScope.Open => query.Where(t => !t.IsArchived
                && t.Status != WorkTaskStatus.Finished
                && t.Status != WorkTaskStatus.Cancelled),
            TaskListScope.Archived => query.Where(t => t.IsArchived),
            _ => query.Where(t => !t.IsArchived),
        };

        return await query
            .OrderBy(t => t.SortKey)
            .Select(ToListItem())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkTaskListItem>> GetOpenAcrossWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        return await db.WorkTasks.AsNoTracking()
            .Where(t => !t.IsArchived
                && t.Status != WorkTaskStatus.Finished
                && t.Status != WorkTaskStatus.Cancelled)
            .OrderBy(t => t.DueDateUtc ?? DateTime.MaxValue)
            .ThenBy(t => t.Priority)
            .Select(ToListItem())
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkTaskDetail?> GetDetailAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await db.WorkTasks.AsNoTracking().IgnoreQueryFilters()
            .Where(t => t.Id == taskId)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.ProjectId,
                ProjectName = t.Project.Name,
                t.Status,
                t.Priority,
                t.StartedAtUtc,
                t.DueDateUtc,
                t.CompletedDateUtc,
                t.CreatedAtUtc,
                t.UpdatedAtUtc,
                t.IsArchived,
                t.IsFavorite,
                t.AssigneePersonId,
                AssigneeName = t.Assignee != null ? t.Assignee.Name : null,
                t.RelatedCompanyId,
                RelatedCompanyName = t.RelatedCompany != null ? t.RelatedCompany.Name : null,
                Tags = t.Tags.Select(x => x.Tag.Name).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return null;
        }

        var subTasks = await db.SubTasks.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.WorkTaskId == taskId && !s.IsDeleted)
            .OrderBy(s => s.SortKey)
            .Select(s => new SubTaskNode
            {
                Id = s.Id,
                ParentSubTaskId = s.ParentSubTaskId,
                Title = s.Title,
                IsDone = s.IsDone,
                SortKey = s.SortKey,
            })
            .ToListAsync(cancellationToken);

        var checklist = await db.ChecklistItems.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.WorkTaskId == taskId && !c.IsDeleted)
            .OrderBy(c => c.SortKey)
            .Select(c => new ChecklistItemView(c.Id, c.Text, c.IsChecked, c.SortKey))
            .ToListAsync(cancellationToken);

        var comments = await db.Comments.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.TargetKind == EntityKind.WorkTask && c.TargetId == taskId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new TaskCommentView(c.Id, c.Body, c.AuthorLabel, c.CreatedAtUtc, c.EditedAtUtc))
            .ToListAsync(cancellationToken);

        var dependsOn = await db.TaskDependencies.AsNoTracking().IgnoreQueryFilters()
            .Where(d => d.WorkTaskId == taskId)
            .Select(d => new DependencyView(
                d.Id,
                d.DependsOnWorkTaskId,
                d.DependsOnWorkTask.Title,
                d.DependsOnWorkTask.Status,
                d.Kind))
            .ToListAsync(cancellationToken);

        return new WorkTaskDetail
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            ProjectName = task.ProjectName,
            Status = task.Status,
            Priority = task.Priority,
            StartedAtUtc = task.StartedAtUtc,
            DueDateUtc = task.DueDateUtc,
            CompletedDateUtc = task.CompletedDateUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            IsArchived = task.IsArchived,
            IsFavorite = task.IsFavorite,
            AssigneePersonId = task.AssigneePersonId,
            AssigneeName = task.AssigneeName,
            RelatedCompanyId = task.RelatedCompanyId,
            RelatedCompanyName = task.RelatedCompanyName,
            SubTasks = subTasks,
            Checklist = checklist,
            Comments = comments,
            DependsOn = dependsOn,
            Tags = task.Tags,
        };
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Tasks.WorkTask, WorkTaskListItem>> ToListItem()
        => t => new WorkTaskListItem
        {
            Id = t.Id,
            Title = t.Title,
            ProjectId = t.ProjectId,
            ProjectName = t.Project.Name,
            ProjectColor = t.Project.Color,
            Status = t.Status,
            Priority = t.Priority,
            DueDateUtc = t.DueDateUtc,
            CompletedDateUtc = t.CompletedDateUtc,
            AssigneeName = t.Assignee != null ? t.Assignee.Name : null,
            RelatedCompanyName = t.RelatedCompany != null ? t.RelatedCompany.Name : null,
            SubTaskTotal = t.SubTasks.Count(s => !s.IsDeleted),
            SubTaskDone = t.SubTasks.Count(s => !s.IsDeleted && s.IsDone),
            ChecklistTotal = t.Checklist.Count(c => !c.IsDeleted),
            ChecklistDone = t.Checklist.Count(c => !c.IsDeleted && c.IsChecked),
            IsArchived = t.IsArchived,
            IsFavorite = t.IsFavorite,
            SortKey = t.SortKey,
        };
}
