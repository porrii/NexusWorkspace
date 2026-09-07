using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.People;

/// <summary>Read-side queries for people. Always projected and no-tracking.</summary>
public sealed class PersonReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<PersonListItem>> GetListAsync(
        PersonScope scope = PersonScope.Active,
        string? search = null,
        Guid? tagId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.People.AsNoTracking().IgnoreQueryFilters().Where(p => !p.IsDeleted);

        query = scope switch
        {
            PersonScope.Active => query.Where(p => !p.IsArchived),
            PersonScope.Archived => query.Where(p => p.IsArchived),
            _ => query,
        };

        if (tagId is { } tag)
        {
            query = query.Where(p => p.Tags.Any(t => t.TagId == tag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%")
                                     || (p.Email != null && EF.Functions.Like(p.Email, $"%{term}%"))
                                     || (p.Role != null && EF.Functions.Like(p.Role, $"%{term}%")));
        }

        return await query
            .OrderByDescending(p => p.IsFavorite)
            .ThenBy(p => p.Name)
            .Take(2000)
            .Select(p => new PersonListItem
            {
                Id = p.Id,
                Name = p.Name,
                Role = p.Role,
                Email = p.Email,
                Phone = p.Phone,
                CompanyId = p.CompanyId,
                CompanyName = p.Company != null ? p.Company.Name : null,
                IsFavorite = p.IsFavorite,
                IsArchived = p.IsArchived,
                LastContactedUtc = p.LastContactedUtc,
                OpenTaskCount = db.WorkTasks.Count(t => !t.IsDeleted
                    && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled
                    && (t.AssigneePersonId == p.Id || t.People.Any(x => x.PersonId == p.Id))),
                ProjectCount = db.Projects.Count(pr => !pr.IsDeleted
                    && (pr.OwnerPersonId == p.Id || pr.People.Any(x => x.PersonId == p.Id))),
                Tags = p.Tags.Select(t => new TagChip { Id = t.Tag.Id, Name = t.Tag.Name, Color = t.Tag.Color }).ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonDetail?> GetDetailAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await db.People.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.Id == personId)
            .Select(p => new PersonDetail
            {
                Id = p.Id,
                Name = p.Name,
                Role = p.Role,
                Email = p.Email,
                Phone = p.Phone,
                Notes = p.Notes,
                CompanyId = p.CompanyId,
                CompanyName = p.Company != null ? p.Company.Name : null,
                IsFavorite = p.IsFavorite,
                IsArchived = p.IsArchived,
                CreatedAtUtc = p.CreatedAtUtc,
                LastContactedUtc = p.LastContactedUtc,
                Tags = p.Tags.Select(t => new TagChip { Id = t.Tag.Id, Name = t.Tag.Name, Color = t.Tag.Color }).ToList(),
                ProjectCount = db.Projects.Count(pr => !pr.IsDeleted
                    && (pr.OwnerPersonId == p.Id || pr.People.Any(x => x.PersonId == p.Id))),
                OpenTaskCount = db.WorkTasks.Count(t => !t.IsDeleted
                    && t.Status != WorkTaskStatus.Finished && t.Status != WorkTaskStatus.Cancelled
                    && (t.AssigneePersonId == p.Id || t.People.Any(x => x.PersonId == p.Id))),
                OpenFollowUpCount = db.FollowUps.Count(f => f.WaitingOnPersonId == p.Id
                    && (f.State == FollowUpState.Waiting || f.State == FollowUpState.Escalated)),
                CommunicationCount = db.Communications.Count(c => !c.IsDeleted && c.PersonId == p.Id),
                MeetingCount = db.Meetings.Count(m => !m.IsDeleted && m.Participants.Any(pp => pp.PersonId == p.Id)),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountAsync(PersonScope scope = PersonScope.Active, CancellationToken cancellationToken = default)
    {
        var query = db.People.AsNoTracking().IgnoreQueryFilters().Where(p => !p.IsDeleted);
        query = scope switch
        {
            PersonScope.Active => query.Where(p => !p.IsArchived),
            PersonScope.Archived => query.Where(p => p.IsArchived),
            _ => query,
        };
        return query.CountAsync(cancellationToken);
    }
}
