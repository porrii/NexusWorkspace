using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Meetings;

namespace NexusWorkspace.Application.Meetings;

/// <summary>
/// Write-side for meetings. Scheduling and holding a meeting append history to the
/// project and to every participating person.
/// </summary>
public sealed class MeetingService(IApplicationDbContext db, IActivityLog activity)
{
    public async Task<Result<Guid>> ScheduleAsync(ScheduleMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Guid>("meeting.title_required", "Indica el título de la reunión.");
        }

        if (request.StartUtc == default)
        {
            return Result.Failure<Guid>("meeting.start_required", "Indica cuándo empieza la reunión.");
        }

        var meeting = new Meeting
        {
            Title = title,
            Agenda = Clean(request.Agenda),
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            Location = Clean(request.Location),
            ProjectId = request.ProjectId,
            Status = MeetingStatus.Scheduled,
        };

        foreach (var input in request.Participants)
        {
            if (input.PersonId is null && string.IsNullOrWhiteSpace(input.ExternalName))
            {
                continue;
            }

            meeting.Participants.Add(new MeetingParticipant
            {
                PersonId = input.PersonId,
                ExternalName = Clean(input.ExternalName),
                Role = Clean(input.Role),
            });
        }

        db.Meetings.Add(meeting);

        activity.Record(EntityKind.Meeting, meeting.Id, ActivityType.MeetingScheduled,
            $"Reunión «{title}» programada para {request.StartUtc:dd/MM/yyyy HH:mm} UTC.", request.ProjectId);

        if (request.ProjectId is { } projectId)
        {
            activity.Record(EntityKind.Project, projectId, ActivityType.MeetingScheduled,
                $"Reunión «{title}» programada.", projectId);
        }

        foreach (var input in request.Participants.Where(p => p.PersonId is not null))
        {
            activity.Record(EntityKind.Person, input.PersonId!.Value, ActivityType.MeetingScheduled,
                $"Convocado a «{title}».", request.ProjectId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return meeting.Id;
    }

    public async Task<Result> UpdateAsync(UpdateMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (meeting is null)
        {
            return NotFound();
        }

        if (request.Title is { } title && !string.IsNullOrWhiteSpace(title))
        {
            meeting.Title = title.Trim();
        }

        if (request.Agenda is not null)
        {
            meeting.Agenda = Clean(request.Agenda);
        }

        if (request.Notes is not null)
        {
            meeting.Notes = Clean(request.Notes);
        }

        if (request.StartUtc is { } start)
        {
            meeting.StartUtc = start;
        }

        if (request.EndUtc is { } end)
        {
            meeting.EndUtc = end;
        }

        if (request.Location is not null)
        {
            meeting.Location = Clean(request.Location);
        }

        activity.Record(EntityKind.Meeting, meeting.Id, ActivityType.MeetingUpdated, $"Reunión «{meeting.Title}» actualizada.", meeting.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkHeldAsync(Guid meetingId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var meeting = await db.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting is null)
        {
            return NotFound();
        }

        meeting.Status = MeetingStatus.Held;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            meeting.Notes = notes.Trim();
        }

        activity.Record(EntityKind.Meeting, meeting.Id, ActivityType.MeetingUpdated, $"Reunión «{meeting.Title}» realizada.", meeting.ProjectId);

        if (meeting.ProjectId is { } projectId)
        {
            activity.Record(EntityKind.Project, projectId, ActivityType.MeetingUpdated, $"Reunión «{meeting.Title}» realizada.", projectId);
        }

        foreach (var participant in meeting.Participants.Where(p => p.PersonId is not null))
        {
            activity.Record(EntityKind.Person, participant.PersonId!.Value, ActivityType.MeetingUpdated,
                $"Participó en «{meeting.Title}».", meeting.ProjectId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting is null)
        {
            return NotFound();
        }

        meeting.Status = MeetingStatus.Cancelled;
        activity.Record(EntityKind.Meeting, meeting.Id, ActivityType.MeetingUpdated, $"Reunión «{meeting.Title}» cancelada.", meeting.ProjectId);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AddParticipantAsync(Guid meetingId, MeetingParticipantInput input, CancellationToken cancellationToken = default)
    {
        var meeting = await db.Meetings.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting is null)
        {
            return NotFound();
        }

        if (input.PersonId is null && string.IsNullOrWhiteSpace(input.ExternalName))
        {
            return Result.Failure("meeting.participant_required", "Indica una persona o un nombre.");
        }

        if (input.PersonId is { } personId && meeting.Participants.Any(p => p.PersonId == personId))
        {
            return Result.Success();
        }

        meeting.Participants.Add(new MeetingParticipant
        {
            PersonId = input.PersonId,
            ExternalName = Clean(input.ExternalName),
            Role = Clean(input.Role),
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var participant = await db.MeetingParticipants.FirstOrDefaultAsync(p => p.Id == participantId, cancellationToken);
        if (participant is null)
        {
            return Result.Success();
        }

        db.MeetingParticipants.Remove(participant);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetAttendedAsync(Guid participantId, bool attended, CancellationToken cancellationToken = default)
    {
        var participant = await db.MeetingParticipants.FirstOrDefaultAsync(p => p.Id == participantId, cancellationToken);
        if (participant is null)
        {
            return Result.Failure("meeting.participant_not_found", "Participante no encontrado.");
        }

        participant.Attended = attended;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("meeting.not_found", "Reunión no encontrada.");
}
