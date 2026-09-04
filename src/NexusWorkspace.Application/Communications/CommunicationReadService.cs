using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Communications;

/// <summary>Read-side access to the communication log.</summary>
public sealed class CommunicationReadService(IApplicationDbContext db)
{
    public Task<IReadOnlyList<CommunicationListItem>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
        => RunAsync(db.Communications.AsNoTracking(), limit, cancellationToken);

    public Task<IReadOnlyList<CommunicationListItem>> GetForPersonAsync(Guid personId, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.Communications.AsNoTracking().Where(c => c.PersonId == personId), limit, cancellationToken);

    public Task<IReadOnlyList<CommunicationListItem>> GetForCompanyAsync(Guid companyId, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.Communications.AsNoTracking().Where(c => c.CompanyId == companyId), limit, cancellationToken);

    public Task<IReadOnlyList<CommunicationListItem>> GetForProjectAsync(Guid projectId, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.Communications.AsNoTracking().Where(c => c.ProjectId == projectId), limit, cancellationToken);

    public Task<IReadOnlyList<CommunicationListItem>> GetForTaskAsync(Guid taskId, int limit = 200, CancellationToken cancellationToken = default)
        => RunAsync(db.Communications.AsNoTracking().Where(c => c.WorkTaskId == taskId), limit, cancellationToken);

    public Task<IReadOnlyList<CommunicationListItem>> GetForEntityAsync(EntityKind kind, Guid id, int limit = 200, CancellationToken cancellationToken = default)
        => kind switch
        {
            EntityKind.Person => GetForPersonAsync(id, limit, cancellationToken),
            EntityKind.Company => GetForCompanyAsync(id, limit, cancellationToken),
            EntityKind.Project => GetForProjectAsync(id, limit, cancellationToken),
            EntityKind.WorkTask => GetForTaskAsync(id, limit, cancellationToken),
            _ => Task.FromResult<IReadOnlyList<CommunicationListItem>>([]),
        };

    public Task<int> CountRecentDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        return db.Communications.AsNoTracking().CountAsync(c => c.OccurredAtUtc >= since, cancellationToken);
    }

    private static async Task<IReadOnlyList<CommunicationListItem>> RunAsync(
        IQueryable<Domain.Communications.Communication> query,
        int limit,
        CancellationToken cancellationToken)
    {
        return await query
            .OrderByDescending(c => c.OccurredAtUtc)
            .Take(Math.Clamp(limit, 1, 2000))
            .Select(c => new CommunicationListItem
            {
                Id = c.Id,
                Channel = c.Channel,
                Direction = c.Direction,
                Subject = c.Subject,
                Body = c.Body,
                OccurredAtUtc = c.OccurredAtUtc,
                PersonId = c.PersonId,
                PersonName = c.Person != null ? c.Person.Name : null,
                CompanyId = c.CompanyId,
                CompanyName = c.Company != null ? c.Company.Name : null,
                ProjectId = c.ProjectId,
                ProjectName = c.Project != null ? c.Project.Name : null,
                WorkTaskId = c.WorkTaskId,
                WorkTaskTitle = c.WorkTask != null ? c.WorkTask.Title : null,
                ContactLabel = c.ContactLabel,
            })
            .ToListAsync(cancellationToken);
    }
}
