using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Tags;

namespace NexusWorkspace.Application.Tags;

/// <summary>Write-side for the tag vocabulary: create, rename, recolour, pin, merge, delete.</summary>
public sealed class TagService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("tag.name_required", "El nombre de la etiqueta es obligatorio.");
        }

        var clash = await db.Tags.AnyAsync(t => t.Name == name, cancellationToken);
        if (clash)
        {
            return Result.Failure<Guid>("tag.duplicate", $"Ya existe la etiqueta «{name}».");
        }

        var tag = new Tag
        {
            Name = name,
            Color = Clean(request.Color),
            Description = Clean(request.Description),
        };

        db.Tags.Add(tag);
        activity.Record(EntityKind.Tag, tag.Id, ActivityType.Created, $"Etiqueta «{tag.Name}» creada.");

        await db.SaveChangesAsync(cancellationToken);
        return tag.Id;
    }

    public async Task<Result> UpdateAsync(UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        if (request.Name is { } newName && !string.IsNullOrWhiteSpace(newName) && newName.Trim() != tag.Name)
        {
            var candidate = newName.Trim();
            var clash = await db.Tags.AnyAsync(t => t.Id != tag.Id && t.Name == candidate, cancellationToken);
            if (clash)
            {
                return Result.Failure("tag.duplicate", $"Ya existe la etiqueta «{candidate}».");
            }

            var old = tag.Name;
            tag.Name = candidate;
            activity.Record(EntityKind.Tag, tag.Id, ActivityType.Renamed, $"Etiqueta «{old}» → «{candidate}».", oldValue: old, newValue: candidate);
        }

        if (request.Color is not null)
        {
            tag.Color = Clean(request.Color);
        }

        if (request.Description is not null)
        {
            tag.Description = Clean(request.Description);
        }

        if (request.IsPinned is { } pinned)
        {
            tag.IsPinned = pinned;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Move every use of <paramref name="sourceId"/> onto <paramref name="targetId"/> and delete the source.</summary>
    public async Task<Result> MergeAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        if (sourceId == targetId)
        {
            return Result.Success();
        }

        var source = await db.Tags.FirstOrDefaultAsync(t => t.Id == sourceId, cancellationToken);
        var target = await db.Tags.FirstOrDefaultAsync(t => t.Id == targetId, cancellationToken);
        if (source is null || target is null)
        {
            return NotFound();
        }

        await MoveLinksAsync(sourceId, targetId, cancellationToken);

        db.Tags.Remove(source);
        activity.Record(EntityKind.Tag, targetId, ActivityType.Updated, $"Etiqueta «{source.Name}» fusionada en «{target.Name}».");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        db.ProjectTags.RemoveRange(db.ProjectTags.Where(x => x.TagId == tagId));
        db.WorkTaskTags.RemoveRange(db.WorkTaskTags.Where(x => x.TagId == tagId));
        db.PersonTags.RemoveRange(db.PersonTags.Where(x => x.TagId == tagId));
        db.CompanyTags.RemoveRange(db.CompanyTags.Where(x => x.TagId == tagId));

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task MoveLinksAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken)
    {
        var projectLinks = await db.ProjectTags.Where(x => x.TagId == sourceId).ToListAsync(cancellationToken);
        foreach (var link in projectLinks)
        {
            if (!await db.ProjectTags.AnyAsync(x => x.ProjectId == link.ProjectId && x.TagId == targetId, cancellationToken))
            {
                db.ProjectTags.Add(new Domain.Projects.ProjectTag { ProjectId = link.ProjectId, TagId = targetId });
            }

            db.ProjectTags.Remove(link);
        }

        var taskLinks = await db.WorkTaskTags.Where(x => x.TagId == sourceId).ToListAsync(cancellationToken);
        foreach (var link in taskLinks)
        {
            if (!await db.WorkTaskTags.AnyAsync(x => x.WorkTaskId == link.WorkTaskId && x.TagId == targetId, cancellationToken))
            {
                db.WorkTaskTags.Add(new Domain.Tasks.WorkTaskTag { WorkTaskId = link.WorkTaskId, TagId = targetId });
            }

            db.WorkTaskTags.Remove(link);
        }

        var personLinks = await db.PersonTags.Where(x => x.TagId == sourceId).ToListAsync(cancellationToken);
        foreach (var link in personLinks)
        {
            if (!await db.PersonTags.AnyAsync(x => x.PersonId == link.PersonId && x.TagId == targetId, cancellationToken))
            {
                db.PersonTags.Add(new Domain.People.PersonTag { PersonId = link.PersonId, TagId = targetId });
            }

            db.PersonTags.Remove(link);
        }

        var companyLinks = await db.CompanyTags.Where(x => x.TagId == sourceId).ToListAsync(cancellationToken);
        foreach (var link in companyLinks)
        {
            if (!await db.CompanyTags.AnyAsync(x => x.CompanyId == link.CompanyId && x.TagId == targetId, cancellationToken))
            {
                db.CompanyTags.Add(new Domain.Companies.CompanyTag { CompanyId = link.CompanyId, TagId = targetId });
            }

            db.CompanyTags.Remove(link);
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("tag.not_found", "Etiqueta no encontrada.");
}
