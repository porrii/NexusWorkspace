using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;

namespace NexusWorkspace.Application.People;

/// <summary>Write-side operations for people. Meaningful changes append history.</summary>
public sealed class PersonService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("person.name_required", "El nombre de la persona es obligatorio.");
        }

        var person = new Person
        {
            Name = name,
            Role = Clean(request.Role),
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Notes = Clean(request.Notes),
            CompanyId = request.CompanyId == Guid.Empty ? null : request.CompanyId,
        };

        db.People.Add(person);
        activity.Record(EntityKind.Person, person.Id, ActivityType.Created, $"Persona «{person.Name}» creada.");

        await db.SaveChangesAsync(cancellationToken);
        return person.Id;
    }

    public async Task<Result> UpdateDetailsAsync(UpdatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        if (request.Name is { } newName && !string.IsNullOrWhiteSpace(newName) && newName.Trim() != person.Name)
        {
            var old = person.Name;
            person.Name = newName.Trim();
            activity.Record(EntityKind.Person, person.Id, ActivityType.Renamed,
                $"Persona renombrada: «{old}» → «{person.Name}».", oldValue: old, newValue: person.Name);
        }

        if (request.Role is not null)
        {
            person.Role = Clean(request.Role);
        }

        if (request.Email is not null)
        {
            person.Email = Clean(request.Email);
        }

        if (request.Phone is not null)
        {
            person.Phone = Clean(request.Phone);
        }

        if (request.Notes is not null)
        {
            person.Notes = Clean(request.Notes);
        }

        if (request.CompanyId is { } companyId && companyId != person.CompanyId)
        {
            person.CompanyId = companyId == Guid.Empty ? null : companyId;
            activity.Record(EntityKind.Person, person.Id, ActivityType.Updated, "Empresa de la persona actualizada.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetFavoriteAsync(Guid personId, bool favorite, CancellationToken cancellationToken = default)
    {
        var person = await db.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        person.IsFavorite = favorite;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid personId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(personId, archived: true, cancellationToken);

    public Task<Result> UnarchiveAsync(Guid personId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(personId, archived: false, cancellationToken);

    public Task<Result> TrashAsync(Guid personId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(personId, trashed: true, cancellationToken);

    public Task<Result> RestoreFromTrashAsync(Guid personId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(personId, trashed: false, cancellationToken);

    public async Task<Result> AddTagAsync(Guid personId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return Result.Failure("tag.not_found", "Etiqueta no encontrada.");
        }

        var exists = await db.PersonTags.AnyAsync(x => x.PersonId == personId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Success();
        }

        db.PersonTags.Add(new PersonTag { PersonId = personId, TagId = tagId });
        activity.Record(EntityKind.Person, personId, ActivityType.TagAdded, $"Etiqueta «{tag.Name}» añadida.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveTagAsync(Guid personId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var link = await db.PersonTags.FirstOrDefaultAsync(x => x.PersonId == personId && x.TagId == tagId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        db.PersonTags.Remove(link);
        activity.Record(EntityKind.Person, personId, ActivityType.TagRemoved, "Etiqueta retirada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetArchivedAsync(Guid personId, bool archived, CancellationToken cancellationToken)
    {
        var person = await db.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        if (person.IsArchived == archived)
        {
            return Result.Success();
        }

        person.IsArchived = archived;
        activity.Record(EntityKind.Person, person.Id,
            archived ? ActivityType.Archived : ActivityType.Restored,
            archived ? $"Persona «{person.Name}» archivada." : $"Persona «{person.Name}» restaurada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetTrashedAsync(Guid personId, bool trashed, CancellationToken cancellationToken)
    {
        var person = await db.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }

        if (person.IsDeleted == trashed)
        {
            return Result.Success();
        }

        person.IsDeleted = trashed;
        activity.Record(EntityKind.Person, person.Id,
            trashed ? ActivityType.Trashed : ActivityType.RestoredFromTrash,
            trashed ? $"Persona «{person.Name}» enviada a la papelera." : $"Persona «{person.Name}» recuperada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("person.not_found", "Persona no encontrada.");
}
