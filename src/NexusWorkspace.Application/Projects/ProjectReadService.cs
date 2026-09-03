using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Projects;

/// <summary>Read-side queries for projects. Always projected and no-tracking.</summary>
public sealed class ProjectReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<ProjectListItem>> GetListAsync(
        ProjectListScope scope = ProjectListScope.Active,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Projects.AsNoTracking().IgnoreQueryFilters().Where(p => !p.IsDeleted);

        query = scope switch
        {
            ProjectListScope.Active => query.Where(p => !p.IsArchived),
            ProjectListScope.Archived => query.Where(p => p.IsArchived),
            _ => query,
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%")
                                     || (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")));
        }

        var rows = await query
            .OrderByDescending(p => p.IsFavorite)
            .ThenByDescending(p => p.LastOpenedAtUtc)
            .ThenByDescending(p => p.UpdatedAtUtc)
            .Select(p => new ProjectListItem
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Icon = p.Icon,
                Color = p.Color,
                Status = p.Status,
                Priority = p.Priority,
                DueDateUtc = p.DueDateUtc,
                IsFavorite = p.IsFavorite,
                IsArchived = p.IsArchived,
                TotalTaskCount = p.Tasks.Count(t => !t.IsDeleted),
                OpenTaskCount = p.Tasks.Count(t => !t.IsDeleted
                    && t.Status != WorkTaskStatus.Finished
                    && t.Status != WorkTaskStatus.Cancelled),
                FinishedTaskCount = p.Tasks.Count(t => !t.IsDeleted && t.Status == WorkTaskStatus.Finished),
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<ProjectDetail?> GetDetailAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await db.Projects.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.Id == projectId)
            .Select(p => new ProjectDetail
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Icon = p.Icon,
                Color = p.Color,
                Status = p.Status,
                Priority = p.Priority,
                StartDateUtc = p.StartDateUtc,
                DueDateUtc = p.DueDateUtc,
                CompletedDateUtc = p.CompletedDateUtc,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc,
                IsArchived = p.IsArchived,
                IsFavorite = p.IsFavorite,
                OwnerPersonId = p.OwnerPersonId,
                OwnerName = p.Owner != null ? p.Owner.Name : null,
                TotalTaskCount = p.Tasks.Count(t => !t.IsDeleted),
                OpenTaskCount = p.Tasks.Count(t => !t.IsDeleted
                    && t.Status != WorkTaskStatus.Finished
                    && t.Status != WorkTaskStatus.Cancelled),
                FinishedTaskCount = p.Tasks.Count(t => !t.IsDeleted && t.Status == WorkTaskStatus.Finished),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
