using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Inbox;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Application.Inbox;

/// <summary>
/// Fast capture and later conversion of <see cref="InboxItem"/>s. Conversions run
/// in one transaction and write the target's first history entry with a
/// "desde Inbox" note. Nothing is lost: dismissed items are soft-deleted.
/// </summary>
public sealed class InboxService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    public async Task<Guid> CaptureAsync(string rawText, string? parsedHint = null, CancellationToken cancellationToken = default)
    {
        var item = new InboxItem
        {
            RawText = (rawText ?? string.Empty).Trim(),
            ParsedHint = string.IsNullOrWhiteSpace(parsedHint) ? null : parsedHint.Trim(),
        };

        db.InboxItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<Result<Guid>> ConvertToTaskAsync(
        Guid inboxItemId,
        Guid projectId,
        ConvertToTaskOptions options,
        CancellationToken cancellationToken = default)
    {
        var item = await db.InboxItems.FirstOrDefaultAsync(i => i.Id == inboxItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<Guid>("inbox.not_found", "El elemento de Inbox ya no existe.");
        }

        if (!await db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            return Result.Failure<Guid>("inbox.project_not_found", "El proyecto indicado no existe.");
        }

        var title = string.IsNullOrWhiteSpace(options.Title) ? item.RawText : options.Title.Trim();

        var maxSortKey = await db.WorkTasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => (double?)t.SortKey)
            .MaxAsync(cancellationToken) ?? 0d;

        var task = new WorkTask
        {
            ProjectId = projectId,
            Title = title,
            Priority = options.Priority,
            DueDateUtc = options.DueDateUtc,
            SortKey = maxSortKey + 1d,
        };
        db.WorkTasks.Add(task);

        activity.Record(EntityKind.WorkTask, task.Id, ActivityType.Created,
            $"Tarea «{task.Title}» creada.", projectId, note: "desde Inbox");

        MarkConverted(item, EntityKind.WorkTask, task.Id);
        await db.SaveChangesAsync(cancellationToken);
        return task.Id;
    }

    public async Task<Result<Guid>> ConvertToProjectAsync(
        Guid inboxItemId,
        ConvertToProjectOptions options,
        CancellationToken cancellationToken = default)
    {
        var item = await db.InboxItems.FirstOrDefaultAsync(i => i.Id == inboxItemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<Guid>("inbox.not_found", "El elemento de Inbox ya no existe.");
        }

        var name = string.IsNullOrWhiteSpace(options.Name) ? item.RawText : options.Name.Trim();

        var project = new Project
        {
            Name = name,
            Priority = options.Priority,
            Status = ProjectStatus.Planning,
        };
        db.Projects.Add(project);

        activity.Record(EntityKind.Project, project.Id, ActivityType.Created,
            $"Proyecto «{project.Name}» creado.", project.Id, note: "desde Inbox");

        MarkConverted(item, EntityKind.Project, project.Id);
        await db.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    public async Task<Result> DismissAsync(Guid inboxItemId, CancellationToken cancellationToken = default)
    {
        var item = await db.InboxItems.FirstOrDefaultAsync(i => i.Id == inboxItemId, cancellationToken);
        if (item is null)
        {
            return Result.Success();
        }

        item.State = InboxItemState.Dismissed;
        item.IsDeleted = true;
        item.ProcessedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private void MarkConverted(InboxItem item, EntityKind kind, Guid id)
    {
        item.State = InboxItemState.Converted;
        item.ConvertedToKind = kind;
        item.ConvertedToId = id;
        item.ProcessedAtUtc = clock.UtcNow;
        item.IsDeleted = true; // leaves the pending list; still queryable in history
    }
}
