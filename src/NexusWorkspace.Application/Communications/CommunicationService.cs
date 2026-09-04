using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Communications;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Communications;

/// <summary>
/// Write-side for logged communications. Logging one appends history to every
/// entity it touches (person, company, project, task) and refreshes their
/// "last contacted" marker.
/// </summary>
public sealed class CommunicationService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    public async Task<Result<Guid>> LogAsync(LogCommunicationRequest request, CancellationToken cancellationToken = default)
    {
        var subject = request.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<Guid>("communication.subject_required", "Indica el asunto de la comunicación.");
        }

        if (request.PersonId is null && request.CompanyId is null
            && request.ProjectId is null && request.WorkTaskId is null
            && string.IsNullOrWhiteSpace(request.ContactLabel))
        {
            return Result.Failure<Guid>("communication.no_target", "Enlaza la comunicación con alguien o algo.");
        }

        var occurredAt = request.OccurredAtUtc ?? clock.UtcNow;

        var projectId = request.ProjectId;
        if (projectId is null && request.WorkTaskId is { } taskId)
        {
            projectId = await db.WorkTasks.Where(t => t.Id == taskId)
                .Select(t => (Guid?)t.ProjectId).FirstOrDefaultAsync(cancellationToken);
        }

        var communication = new Communication
        {
            Channel = request.Channel,
            Direction = request.Direction,
            Subject = subject,
            Body = Clean(request.Body),
            OccurredAtUtc = occurredAt,
            PersonId = request.PersonId,
            CompanyId = request.CompanyId,
            ProjectId = projectId,
            WorkTaskId = request.WorkTaskId,
            ContactLabel = Clean(request.ContactLabel),
        };

        db.Communications.Add(communication);

        var headline = $"{DisplayNames.Of(request.Channel)} · {DisplayNames.Of(request.Direction)}: {subject}";

        if (request.PersonId is { } personId)
        {
            activity.Record(EntityKind.Person, personId, ActivityType.CommunicationLogged, headline, projectId, note: Clean(request.Body));
            await TouchLastContactedAsync(personId, isPerson: true, occurredAt, cancellationToken);
        }

        if (request.CompanyId is { } companyId)
        {
            activity.Record(EntityKind.Company, companyId, ActivityType.CommunicationLogged, headline, projectId, note: Clean(request.Body));
            await TouchLastContactedAsync(companyId, isPerson: false, occurredAt, cancellationToken);
        }

        if (request.WorkTaskId is { } wtId)
        {
            activity.Record(EntityKind.WorkTask, wtId, ActivityType.CommunicationLogged, headline, projectId, note: Clean(request.Body));
        }
        else if (projectId is { } pid)
        {
            activity.Record(EntityKind.Project, pid, ActivityType.CommunicationLogged, headline, pid, note: Clean(request.Body));
        }

        await db.SaveChangesAsync(cancellationToken);
        return communication.Id;
    }

    public async Task<Result> UpdateAsync(UpdateCommunicationRequest request, CancellationToken cancellationToken = default)
    {
        var communication = await db.Communications.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (communication is null)
        {
            return NotFound();
        }

        if (request.Channel is { } channel)
        {
            communication.Channel = channel;
        }

        if (request.Direction is { } direction)
        {
            communication.Direction = direction;
        }

        if (request.Subject is { } subject && !string.IsNullOrWhiteSpace(subject))
        {
            communication.Subject = subject.Trim();
        }

        if (request.Body is not null)
        {
            communication.Body = Clean(request.Body);
        }

        if (request.OccurredAtUtc is { } occurredAt)
        {
            communication.OccurredAtUtc = occurredAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid communicationId, CancellationToken cancellationToken = default)
    {
        var communication = await db.Communications.FirstOrDefaultAsync(c => c.Id == communicationId, cancellationToken);
        if (communication is null)
        {
            return NotFound();
        }

        communication.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task TouchLastContactedAsync(Guid id, bool isPerson, DateTime occurredAt, CancellationToken cancellationToken)
    {
        if (isPerson)
        {
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (person is not null && (person.LastContactedUtc is null || occurredAt > person.LastContactedUtc))
            {
                person.LastContactedUtc = occurredAt;
            }

            return;
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (company is not null && (company.LastContactedUtc is null || occurredAt > company.LastContactedUtc))
        {
            company.LastContactedUtc = occurredAt;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("communication.not_found", "Comunicación no encontrada.");
}
