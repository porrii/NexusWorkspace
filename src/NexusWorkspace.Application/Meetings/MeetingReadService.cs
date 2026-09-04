using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Meetings;

/// <summary>Read-side access to meetings.</summary>
public sealed class MeetingReadService(IApplicationDbContext db, IClock clock)
{
    public Task<IReadOnlyList<MeetingListItem>> GetForProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        => RunAsync(db.Meetings.AsNoTracking().Where(m => m.ProjectId == projectId), cancellationToken);

    public Task<IReadOnlyList<MeetingListItem>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
        => RunAsync(db.Meetings.AsNoTracking().Where(m => m.Participants.Any(p => p.PersonId == personId)), cancellationToken);

    public Task<IReadOnlyList<MeetingListItem>> GetRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        => RunAsync(
            db.Meetings.AsNoTracking().Where(m => m.Status != MeetingStatus.Cancelled && m.StartUtc >= fromUtc && m.StartUtc < toUtc),
            cancellationToken);

    public async Task<IReadOnlyList<MeetingListItem>> GetUpcomingAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        return await Project(db.Meetings.AsNoTracking()
                .Where(m => m.Status == MeetingStatus.Scheduled && m.StartUtc >= now)
                .OrderBy(m => m.StartUtc)
                .Take(Math.Clamp(limit, 1, 200)))
            .ToListAsync(cancellationToken);
    }

    public Task<MeetingListItem?> GetDetailAsync(Guid meetingId, CancellationToken cancellationToken = default)
        => Project(db.Meetings.AsNoTracking().Where(m => m.Id == meetingId)).FirstOrDefaultAsync(cancellationToken);

    private static async Task<IReadOnlyList<MeetingListItem>> RunAsync(
        IQueryable<Domain.Meetings.Meeting> query,
        CancellationToken cancellationToken)
    {
        return await Project(query.OrderByDescending(m => m.StartUtc)).ToListAsync(cancellationToken);
    }

    private static IQueryable<MeetingListItem> Project(IQueryable<Domain.Meetings.Meeting> query)
        => query.Select(m => new MeetingListItem
        {
            Id = m.Id,
            Title = m.Title,
            Agenda = m.Agenda,
            Notes = m.Notes,
            StartUtc = m.StartUtc,
            EndUtc = m.EndUtc,
            Location = m.Location,
            Status = m.Status,
            ProjectId = m.ProjectId,
            ProjectName = m.Project != null ? m.Project.Name : null,
            ParticipantCount = m.Participants.Count,
            Participants = m.Participants.Select(p => new MeetingParticipantView
            {
                Id = p.Id,
                PersonId = p.PersonId,
                Name = p.Person != null ? p.Person.Name : (p.ExternalName ?? "—"),
                Role = p.Role,
                Attended = p.Attended,
            }).ToList(),
        });
}
