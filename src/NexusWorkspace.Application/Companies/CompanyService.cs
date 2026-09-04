using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Companies;

/// <summary>Write-side operations for companies. Meaningful changes append history.</summary>
public sealed class CompanyService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Guid>("company.name_required", "El nombre de la empresa es obligatorio.");
        }

        var company = new Company
        {
            Name = name,
            Kind = request.Kind,
            Website = Clean(request.Website),
            Notes = Clean(request.Notes),
        };

        db.Companies.Add(company);
        activity.Record(EntityKind.Company, company.Id, ActivityType.Created, $"Empresa «{company.Name}» creada.");

        await db.SaveChangesAsync(cancellationToken);
        return company.Id;
    }

    public async Task<Result> UpdateDetailsAsync(UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (request.Name is { } newName && !string.IsNullOrWhiteSpace(newName) && newName.Trim() != company.Name)
        {
            var old = company.Name;
            company.Name = newName.Trim();
            activity.Record(EntityKind.Company, company.Id, ActivityType.Renamed,
                $"Empresa renombrada: «{old}» → «{company.Name}».", oldValue: old, newValue: company.Name);
        }

        if (request.Kind is { } kind)
        {
            company.Kind = kind;
        }

        if (request.Website is not null)
        {
            company.Website = Clean(request.Website);
        }

        if (request.Notes is not null)
        {
            company.Notes = Clean(request.Notes);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetFavoriteAsync(Guid companyId, bool favorite, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.IsFavorite = favorite;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> ArchiveAsync(Guid companyId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(companyId, archived: true, cancellationToken);

    public Task<Result> UnarchiveAsync(Guid companyId, CancellationToken cancellationToken = default)
        => SetArchivedAsync(companyId, archived: false, cancellationToken);

    public Task<Result> TrashAsync(Guid companyId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(companyId, trashed: true, cancellationToken);

    public Task<Result> RestoreFromTrashAsync(Guid companyId, CancellationToken cancellationToken = default)
        => SetTrashedAsync(companyId, trashed: false, cancellationToken);

    public async Task<Result> AddTagAsync(Guid companyId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return Result.Failure("tag.not_found", "Etiqueta no encontrada.");
        }

        var exists = await db.CompanyTags.AnyAsync(x => x.CompanyId == companyId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Success();
        }

        db.CompanyTags.Add(new CompanyTag { CompanyId = companyId, TagId = tagId });
        activity.Record(EntityKind.Company, companyId, ActivityType.TagAdded, $"Etiqueta «{tag.Name}» añadida.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveTagAsync(Guid companyId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var link = await db.CompanyTags.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.TagId == tagId, cancellationToken);
        if (link is null)
        {
            return Result.Success();
        }

        db.CompanyTags.Remove(link);
        activity.Record(EntityKind.Company, companyId, ActivityType.TagRemoved, "Etiqueta retirada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetArchivedAsync(Guid companyId, bool archived, CancellationToken cancellationToken)
    {
        var company = await db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (company.IsArchived == archived)
        {
            return Result.Success();
        }

        company.IsArchived = archived;
        activity.Record(EntityKind.Company, company.Id,
            archived ? ActivityType.Archived : ActivityType.Restored,
            archived ? $"Empresa «{company.Name}» archivada." : $"Empresa «{company.Name}» restaurada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> SetTrashedAsync(Guid companyId, bool trashed, CancellationToken cancellationToken)
    {
        var company = await db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (company.IsDeleted == trashed)
        {
            return Result.Success();
        }

        company.IsDeleted = trashed;
        activity.Record(EntityKind.Company, company.Id,
            trashed ? ActivityType.Trashed : ActivityType.RestoredFromTrash,
            trashed ? $"Empresa «{company.Name}» enviada a la papelera." : $"Empresa «{company.Name}» recuperada.");

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("company.not_found", "Empresa no encontrada.");
}
