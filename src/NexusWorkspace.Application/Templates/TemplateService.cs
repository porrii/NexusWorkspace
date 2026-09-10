using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tags;
using NexusWorkspace.Domain.Tasks;
using NexusWorkspace.Domain.Templates;

namespace NexusWorkspace.Application.Templates;

/// <summary>
/// Captures a project or task as a reusable tree and re-creates it on demand.
/// Applying a template appends <see cref="ActivityType.TemplateApplied"/> history.
/// </summary>
public sealed class TemplateService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<Guid>> CreateFromProjectAsync(Guid projectId, string name, CancellationToken cancellationToken = default)
    {
        var templateName = name?.Trim();
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return Result.Failure<Guid>("template.name_required", "Ponle un nombre a la plantilla.");
        }

        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Guid>("project.not_found", "Proyecto no encontrado.");
        }

        var tasks = await db.WorkTasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortKey)
            .Select(t => new { t.Id, t.Title, t.Priority })
            .ToListAsync(cancellationToken);

        var subTasks = await db.SubTasks.AsNoTracking()
            .Where(s => s.WorkTask.ProjectId == projectId)
            .OrderBy(s => s.SortKey)
            .Select(s => new SubRow(s.Id, s.WorkTaskId, s.ParentSubTaskId, s.Title))
            .ToListAsync(cancellationToken);

        var checklist = await db.ChecklistItems.AsNoTracking()
            .Where(c => c.WorkTask.ProjectId == projectId)
            .OrderBy(c => c.SortKey)
            .Select(c => new { c.WorkTaskId, c.Text })
            .ToListAsync(cancellationToken);

        var taskTags = await db.WorkTaskTags.AsNoTracking()
            .Where(x => x.WorkTask.ProjectId == projectId)
            .Select(x => new { x.WorkTaskId, x.Tag.Name })
            .ToListAsync(cancellationToken);

        var projectTags = await db.ProjectTags.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .Select(x => x.Tag.Name)
            .ToListAsync(cancellationToken);

        var definition = new ProjectTemplateDefinition
        {
            Description = project.Description,
            Tags = projectTags,
            Tasks = tasks.Select(t => new TaskNodeDef
            {
                Title = t.Title,
                Priority = t.Priority,
                SubTasks = BuildSubTree(t.Id, null),
                Checklist = checklist.Where(c => c.WorkTaskId == t.Id).Select(c => c.Text).ToList(),
                Tags = taskTags.Where(x => x.WorkTaskId == t.Id).Select(x => x.Name).ToList(),
            }).ToList(),
        };

        var template = new Template
        {
            Name = templateName,
            Kind = TemplateKind.Project,
            Description = project.Description,
            DefinitionJson = JsonSerializer.Serialize(definition, Json),
        };

        db.Templates.Add(template);
        activity.Record(EntityKind.Project, projectId, ActivityType.TemplateApplied, $"Guardado como plantilla «{templateName}».", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return template.Id;

        IReadOnlyList<SubTaskNodeDef> BuildSubTree(Guid workTaskId, Guid? parentId) => subTasks
            .Where(s => s.WorkTaskId == workTaskId && s.ParentSubTaskId == parentId)
            .Select(s => new SubTaskNodeDef { Title = s.Title, Children = BuildSubTree(workTaskId, s.Id) })
            .ToList();
    }

    private sealed record SubRow(Guid Id, Guid WorkTaskId, Guid? ParentSubTaskId, string Title);

    public async Task<Result<Guid>> CreateFromTaskAsync(Guid taskId, string name, CancellationToken cancellationToken = default)
    {
        var templateName = name?.Trim();
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return Result.Failure<Guid>("template.name_required", "Ponle un nombre a la plantilla.");
        }

        var task = await db.WorkTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Guid>("task.not_found", "Tarea no encontrada.");
        }

        var subTasks = await db.SubTasks.AsNoTracking()
            .Where(s => s.WorkTaskId == taskId)
            .Select(s => new { s.Id, s.ParentSubTaskId, s.Title })
            .ToListAsync(cancellationToken);

        var checklist = await db.ChecklistItems.AsNoTracking()
            .Where(c => c.WorkTaskId == taskId).OrderBy(c => c.SortKey)
            .Select(c => c.Text).ToListAsync(cancellationToken);

        var tags = await db.WorkTaskTags.AsNoTracking()
            .Where(x => x.WorkTaskId == taskId).Select(x => x.Tag.Name).ToListAsync(cancellationToken);

        var definition = new TaskTemplateDefinition
        {
            Description = task.Description,
            Priority = task.Priority,
            SubTasks = BuildTree(null),
            Checklist = checklist,
            Tags = tags,
        };

        var template = new Template
        {
            Name = templateName,
            Kind = TemplateKind.Task,
            Description = task.Description,
            DefinitionJson = JsonSerializer.Serialize(definition, Json),
        };

        db.Templates.Add(template);
        activity.Record(EntityKind.WorkTask, taskId, ActivityType.TemplateApplied, $"Guardada como plantilla «{templateName}».", task.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return template.Id;

        IReadOnlyList<SubTaskNodeDef> BuildTree(Guid? parentId) => subTasks
            .Where(s => s.ParentSubTaskId == parentId)
            .Select(s => new SubTaskNodeDef { Title = s.Title, Children = BuildTree(s.Id) })
            .ToList();
    }

    public async Task<Result<Guid>> ApplyProjectTemplateAsync(Guid templateId, string projectName, CancellationToken cancellationToken = default)
    {
        var name = projectName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("project.name_required", "Escribe un nombre para el proyecto.");
        }

        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null || template.Kind != TemplateKind.Project)
        {
            return Result.Failure<Guid>("template.not_found", "Plantilla de proyecto no encontrada.");
        }

        var definition = JsonSerializer.Deserialize<ProjectTemplateDefinition>(template.DefinitionJson, Json)
                         ?? new ProjectTemplateDefinition();

        var project = new Project { Name = name, Description = Clean(definition.Description), Status = ProjectStatus.Planning };
        db.Projects.Add(project);

        var tagCache = await LoadTagCacheAsync(cancellationToken);
        foreach (var tagName in definition.Tags)
        {
            db.ProjectTags.Add(new ProjectTag { ProjectId = project.Id, TagId = EnsureTag(tagName, tagCache) });
        }

        var sortKey = 1d;
        foreach (var node in definition.Tasks)
        {
            var task = new WorkTask
            {
                ProjectId = project.Id,
                Title = string.IsNullOrWhiteSpace(node.Title) ? "(sin título)" : node.Title.Trim(),
                Priority = node.Priority,
                SortKey = sortKey++,
            };
            db.WorkTasks.Add(task);

            AddSubTasks(task.Id, node.SubTasks, null);

            var checkKey = 1000d;
            foreach (var text in node.Checklist.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                db.SubTasks.Add(new SubTask { WorkTaskId = task.Id, Title = text.Trim(), SortKey = checkKey++ });
            }

            foreach (var tagName in node.Tags)
            {
                db.WorkTaskTags.Add(new WorkTaskTag { WorkTaskId = task.Id, TagId = EnsureTag(tagName, tagCache) });
            }
        }

        template.UseCount++;
        template.LastUsedAtUtc = clock.UtcNow;

        activity.Record(EntityKind.Project, project.Id, ActivityType.TemplateApplied,
            $"Proyecto creado desde la plantilla «{template.Name}».", project.Id);

        await db.SaveChangesAsync(cancellationToken);
        return project.Id;

        void AddSubTasks(Guid taskId, IReadOnlyList<SubTaskNodeDef> nodes, Guid? parentId)
        {
            var key = 1d;
            foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Title)))
            {
                var sub = new SubTask
                {
                    WorkTaskId = taskId,
                    ParentSubTaskId = parentId,
                    Title = node.Title.Trim(),
                    SortKey = key++,
                };
                db.SubTasks.Add(sub);
                AddSubTasks(taskId, node.Children, sub.Id);
            }
        }
    }

    public async Task<Result<Guid>> ApplyTaskTemplateAsync(Guid templateId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null || template.Kind != TemplateKind.Task)
        {
            return Result.Failure<Guid>("template.not_found", "Plantilla de tarea no encontrada.");
        }

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Guid>("project.not_found", "Proyecto no encontrado.");
        }

        var definition = JsonSerializer.Deserialize<TaskTemplateDefinition>(template.DefinitionJson, Json)
                         ?? new TaskTemplateDefinition();

        var nextSort = await db.WorkTasks.Where(t => t.ProjectId == projectId)
            .Select(t => (double?)t.SortKey).MaxAsync(cancellationToken) ?? 0d;

        var task = new WorkTask
        {
            ProjectId = projectId,
            Title = template.Name,
            Description = Clean(definition.Description),
            Priority = definition.Priority,
            SortKey = nextSort + 1d,
        };
        db.WorkTasks.Add(task);

        var tagCache = await LoadTagCacheAsync(cancellationToken);
        foreach (var tagName in definition.Tags)
        {
            db.WorkTaskTags.Add(new WorkTaskTag { WorkTaskId = task.Id, TagId = EnsureTag(tagName, tagCache) });
        }

        var checkKey = 1000d;
        foreach (var text in definition.Checklist.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            db.SubTasks.Add(new SubTask { WorkTaskId = task.Id, Title = text.Trim(), SortKey = checkKey++ });
        }

        AddSubTasks(definition.SubTasks, null);

        template.UseCount++;
        template.LastUsedAtUtc = clock.UtcNow;

        activity.Record(EntityKind.WorkTask, task.Id, ActivityType.TemplateApplied,
            $"Tarea creada desde la plantilla «{template.Name}».", projectId);

        await db.SaveChangesAsync(cancellationToken);
        return task.Id;

        void AddSubTasks(IReadOnlyList<SubTaskNodeDef> nodes, Guid? parentId)
        {
            var key = 1d;
            foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Title)))
            {
                var sub = new SubTask
                {
                    WorkTaskId = task.Id,
                    ParentSubTaskId = parentId,
                    Title = node.Title.Trim(),
                    SortKey = key++,
                };
                db.SubTasks.Add(sub);
                AddSubTasks(node.Children, sub.Id);
            }
        }
    }

    public async Task<Result> RenameAsync(Guid templateId, string name, CancellationToken cancellationToken = default)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure("template.not_found", "Plantilla no encontrada.");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            template.Name = name.Trim();
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null)
        {
            return Result.Success();
        }

        db.Templates.Remove(template);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Dictionary<string, Guid>> LoadTagCacheAsync(CancellationToken cancellationToken)
        => await db.Tags.ToDictionaryAsync(t => t.Name.ToLowerInvariant(), t => t.Id, cancellationToken);

    private Guid EnsureTag(string name, Dictionary<string, Guid> cache)
    {
        var trimmed = name.Trim();
        var key = trimmed.ToLowerInvariant();
        if (cache.TryGetValue(key, out var id))
        {
            return id;
        }

        var tag = new Tag { Name = trimmed };
        db.Tags.Add(tag);
        cache[key] = tag.Id;
        return tag.Id;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
