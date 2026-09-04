using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Relations;

namespace NexusWorkspace.Application.Relations;

/// <summary>Write-side for typed cross-references between entities.</summary>
public sealed class RelationService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result<Guid>> AddAsync(AddRelationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromKind == request.ToKind && request.FromId == request.ToId)
        {
            return Result.Failure<Guid>("relation.self", "Una entidad no puede relacionarse consigo misma.");
        }

        var duplicate = await db.EntityRelations.AnyAsync(r =>
            r.Kind == request.Kind
            && ((r.FromKind == request.FromKind && r.FromId == request.FromId && r.ToKind == request.ToKind && r.ToId == request.ToId)
                || (r.FromKind == request.ToKind && r.FromId == request.ToId && r.ToKind == request.FromKind && r.ToId == request.FromId)),
            cancellationToken);
        if (duplicate)
        {
            return Result.Failure<Guid>("relation.duplicate", "Esa relación ya existe.");
        }

        var relation = new EntityRelation
        {
            FromKind = request.FromKind,
            FromId = request.FromId,
            ToKind = request.ToKind,
            ToId = request.ToId,
            Kind = request.Kind,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        db.EntityRelations.Add(relation);

        activity.Record(request.FromKind, request.FromId, ActivityType.RelationAdded, "Relación añadida.");
        activity.Record(request.ToKind, request.ToId, ActivityType.RelationAdded, "Relación añadida.");

        await db.SaveChangesAsync(cancellationToken);
        return relation.Id;
    }

    public async Task<Result> RemoveAsync(Guid relationId, CancellationToken cancellationToken = default)
    {
        var relation = await db.EntityRelations.FirstOrDefaultAsync(r => r.Id == relationId, cancellationToken);
        if (relation is null)
        {
            return Result.Success();
        }

        relation.IsDeleted = true;
        activity.Record(relation.FromKind, relation.FromId, ActivityType.RelationRemoved, "Relación eliminada.");
        activity.Record(relation.ToKind, relation.ToId, ActivityType.RelationRemoved, "Relación eliminada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
