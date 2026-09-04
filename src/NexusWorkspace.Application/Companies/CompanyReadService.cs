using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.People;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Companies;

/// <summary>Read-side queries for companies. Always projected and no-tracking.</summary>
public sealed class CompanyReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<CompanyListItem>> GetListAsync(
        CompanyScope scope = CompanyScope.Active,
        string? search = null,
        Guid? tagId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Companies.AsNoTracking().IgnoreQueryFilters().Where(c => !c.IsDeleted);

        query = scope switch
        {
            CompanyScope.Active => query.Where(c => !c.IsArchived),
            CompanyScope.Archived => query.Where(c => c.IsArchived),
            _ => query,
        };

        if (tagId is { } tag)
        {
            query = query.Where(c => c.Tags.Any(t => t.TagId == tag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%")
                                     || (c.Website != null && EF.Functions.Like(c.Website, $"%{term}%")));
        }

        return await query
            .OrderByDescending(c => c.IsFavorite)
            .ThenBy(c => c.Name)
            .Select(c => new CompanyListItem
            {
                Id = c.Id,
                Name = c.Name,
                Kind = c.Kind,
                Website = c.Website,
                IsFavorite = c.IsFavorite,
                IsArchived = c.IsArchived,
                LastContactedUtc = c.LastContactedUtc,
                PeopleCount = db.People.Count(p => !p.IsDeleted && p.CompanyId == c.Id),
                ProjectCount = db.Projects.Count(pr => !pr.IsDeleted && pr.Companies.Any(x => x.CompanyId == c.Id)),
                Tags = c.Tags.Select(t => new TagChip { Id = t.Tag.Id, Name = t.Tag.Name, Color = t.Tag.Color }).ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyDetail?> GetDetailAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await db.Companies.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Id == companyId)
            .Select(c => new CompanyDetail
            {
                Id = c.Id,
                Name = c.Name,
                Kind = c.Kind,
                Website = c.Website,
                Notes = c.Notes,
                IsFavorite = c.IsFavorite,
                IsArchived = c.IsArchived,
                CreatedAtUtc = c.CreatedAtUtc,
                LastContactedUtc = c.LastContactedUtc,
                Tags = c.Tags.Select(t => new TagChip { Id = t.Tag.Id, Name = t.Tag.Name, Color = t.Tag.Color }).ToList(),
                PeopleCount = db.People.Count(p => !p.IsDeleted && p.CompanyId == c.Id),
                ProjectCount = db.Projects.Count(pr => !pr.IsDeleted && pr.Companies.Any(x => x.CompanyId == c.Id)),
                OpenTaskCount = db.WorkTasks.Count(t => !t.IsDeleted
                    && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled
                    && (t.RelatedCompanyId == c.Id || t.Project.Companies.Any(x => x.CompanyId == c.Id))),
                OpenFollowUpCount = db.FollowUps.Count(f => f.WaitingOnCompanyId == c.Id
                    && (f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated)),
                CommunicationCount = db.Communications.Count(cm => !cm.IsDeleted && cm.CompanyId == c.Id),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountAsync(CompanyScope scope = CompanyScope.Active, CancellationToken cancellationToken = default)
    {
        var query = db.Companies.AsNoTracking().IgnoreQueryFilters().Where(c => !c.IsDeleted);
        query = scope switch
        {
            CompanyScope.Active => query.Where(c => !c.IsArchived),
            CompanyScope.Archived => query.Where(c => c.IsArchived),
            _ => query,
        };
        return query.CountAsync(cancellationToken);
    }
}
