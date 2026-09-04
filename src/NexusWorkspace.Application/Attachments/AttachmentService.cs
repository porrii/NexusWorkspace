using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Collaboration;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Attachments;

/// <summary>
/// Write-side for attachments. The bytes go to the external <see cref="IAttachmentStore"/>;
/// only metadata lands in the database. Adding and removing append history.
/// </summary>
public sealed class AttachmentService(IApplicationDbContext db, IAttachmentStore store, IActivityLog activity)
{
    public async Task<Result<Guid>> AddAsync(
        EntityKind targetKind,
        Guid targetId,
        string fileName,
        Stream content,
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var name = fileName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("attachment.name_required", "El archivo necesita un nombre.");
        }

        if (content is null)
        {
            return Result.Failure<Guid>("attachment.no_content", "No hay contenido que guardar.");
        }

        var resolvedProjectId = projectId ?? await ResolveProjectIdAsync(targetKind, targetId, cancellationToken);

        var stored = await store.SaveAsync(content, name, cancellationToken);

        var attachment = new Attachment
        {
            TargetKind = targetKind,
            TargetId = targetId,
            ProjectId = resolvedProjectId,
            FileName = name,
            RelativePath = stored.RelativePath,
            MimeType = FileKinds.MimeOf(name),
            SizeBytes = stored.SizeBytes,
            ContentHash = stored.ContentHash,
        };

        db.Attachments.Add(attachment);
        activity.Record(targetKind, targetId, ActivityType.AttachmentAdded, $"Adjuntado «{name}».", resolvedProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return attachment.Id;
    }

    public async Task<Result> RenameAsync(Guid attachmentId, string newName, CancellationToken cancellationToken = default)
    {
        var name = newName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("attachment.name_required", "El archivo necesita un nombre.");
        }

        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null)
        {
            return Result.Failure("attachment.not_found", "Adjunto no encontrado.");
        }

        attachment.FileName = name;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null)
        {
            return Result.Success();
        }

        attachment.IsDeleted = true;
        activity.Record(attachment.TargetKind, attachment.TargetId, ActivityType.AttachmentRemoved,
            $"Quitado «{attachment.FileName}».", attachment.ProjectId);

        await db.SaveChangesAsync(cancellationToken);

        // Content-addressed store: only delete the physical file when nothing live points at it.
        if (!string.IsNullOrEmpty(attachment.ContentHash))
        {
            var stillUsed = await db.Attachments
                .AnyAsync(a => !a.IsDeleted && a.ContentHash == attachment.ContentHash, cancellationToken);
            if (!stillUsed)
            {
                store.Delete(attachment.RelativePath);
            }
        }

        return Result.Success();
    }

    private async Task<Guid?> ResolveProjectIdAsync(EntityKind kind, Guid id, CancellationToken cancellationToken) => kind switch
    {
        EntityKind.Project => id,
        EntityKind.WorkTask => await db.WorkTasks.Where(t => t.Id == id)
            .Select(t => (Guid?)t.ProjectId).FirstOrDefaultAsync(cancellationToken),
        _ => null,
    };
}
