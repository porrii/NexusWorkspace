using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Attachments;

/// <summary>Read-side for attachments. Resolves each row's absolute path from the store.</summary>
public sealed class AttachmentReadService(IApplicationDbContext db, IAttachmentStore store)
{
    public Task<IReadOnlyList<AttachmentListItem>> GetForEntityAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
        => RunAsync(db.Attachments.AsNoTracking().Where(a => a.TargetKind == kind && a.TargetId == id), cancellationToken);

    public Task<IReadOnlyList<AttachmentListItem>> GetForProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        => RunAsync(db.Attachments.AsNoTracking().Where(a => a.ProjectId == projectId), cancellationToken);

    public async Task<IReadOnlyList<AttachmentListItem>> GetAllAsync(AttachmentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Attachments.AsNoTracking().AsQueryable();

        if (filter.ProjectId is { } projectId)
        {
            query = query.Where(a => a.ProjectId == projectId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(a => EF.Functions.Like(a.FileName, $"%{term}%"));
        }

        var rows = await RunAsync(query, cancellationToken);

        if (filter.ImagesOnly)
        {
            rows = rows.Where(r => r.IsImage).ToList();
        }
        else if (filter.Kind is { } kind)
        {
            rows = rows.Where(r => r.Kind == kind).ToList();
        }

        return rows;
    }

    public Task<int> CountForEntityAsync(EntityKind kind, Guid id, CancellationToken cancellationToken = default)
        => db.Attachments.AsNoTracking().CountAsync(a => a.TargetKind == kind && a.TargetId == id, cancellationToken);

    private async Task<IReadOnlyList<AttachmentListItem>> RunAsync(
        IQueryable<Domain.Collaboration.Attachment> query,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from a in query.OrderByDescending(x => x.CreatedAtUtc).Take(2000)
            join p in db.Projects on a.ProjectId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            orderby a.CreatedAtUtc descending
            select new
            {
                a.Id,
                a.FileName,
                a.RelativePath,
                a.MimeType,
                a.SizeBytes,
                a.TargetKind,
                a.TargetId,
                a.ProjectId,
                ProjectName = p != null ? p.Name : null,
                a.CreatedAtUtc,
            }).ToListAsync(cancellationToken);

        return rows.Select(r => new AttachmentListItem
        {
            Id = r.Id,
            FileName = r.FileName,
            RelativePath = r.RelativePath,
            AbsolutePath = store.GetAbsolutePath(r.RelativePath),
            MimeType = r.MimeType,
            SizeBytes = r.SizeBytes,
            Kind = FileKinds.KindOf(r.FileName),
            IsImage = FileKinds.IsImage(r.FileName),
            TargetKind = r.TargetKind,
            TargetId = r.TargetId,
            ProjectId = r.ProjectId,
            ProjectName = r.ProjectName,
            CreatedAtUtc = r.CreatedAtUtc,
        }).ToList();
    }
}
