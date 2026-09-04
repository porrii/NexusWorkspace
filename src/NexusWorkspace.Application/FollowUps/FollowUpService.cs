using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.FollowUps;

namespace NexusWorkspace.Application.FollowUps;

/// <summary>
/// Write-side for "waiting for a reply from…" follow-ups. Every step appends to
/// the target's history (started, reminder sent, resolved).
/// </summary>
public sealed class FollowUpService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    private static readonly TimeSpan DefaultNudgeInterval = TimeSpan.FromDays(3);

    public async Task<Result<Guid>> StartAsync(StartFollowUpRequest request, CancellationToken cancellationToken = default)
    {
        var subject = request.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<Guid>("followup.subject_required", "Indica qué estás esperando.");
        }

        var projectId = await ResolveProjectIdAsync(request.TargetKind, request.TargetId, cancellationToken);
        var now = clock.UtcNow;

        // Always keep a denormalised display label for the party we are chasing.
        var label = Clean(request.WaitingOnLabel);
        if (label is null && request.WaitingOnPersonId is { } personId)
        {
            label = await db.People.Where(p => p.Id == personId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);
        }

        if (label is null && request.WaitingOnCompanyId is { } companyId)
        {
            label = await db.Companies.Where(c => c.Id == companyId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var followUp = new FollowUp
        {
            TargetKind = request.TargetKind,
            TargetId = request.TargetId,
            ProjectId = projectId,
            Subject = subject,
            WaitingOnPersonId = request.WaitingOnPersonId,
            WaitingOnCompanyId = request.WaitingOnCompanyId,
            WaitingOnLabel = label,
            WaitingSinceUtc = now,
            LastContactUtc = now,
            NextFollowUpUtc = request.NextFollowUpUtc ?? now.Add(DefaultNudgeInterval),
            State = FollowUpState.Waiting,
        };

        db.FollowUps.Add(followUp);
        activity.Record(request.TargetKind, request.TargetId, ActivityType.FollowUpStarted,
            $"Esperando respuesta: {subject}.", projectId, note: WaitingOnText(followUp));

        await db.SaveChangesAsync(cancellationToken);
        return followUp.Id;
    }

    public async Task<Result> SendReminderAsync(Guid followUpId, string? note = null, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        var now = clock.UtcNow;
        followUp.ReminderCount++;
        followUp.LastContactUtc = now;
        followUp.NextFollowUpUtc = now.Add(DefaultNudgeInterval);

        activity.Record(followUp.TargetKind, followUp.TargetId, ActivityType.FollowUpReminderSent,
            $"Recordatorio #{followUp.ReminderCount} a {WaitingOnText(followUp)}.",
            followUp.ProjectId, note: note);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RecordContactAsync(Guid followUpId, string? note = null, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        followUp.LastContactUtc = clock.UtcNow;
        activity.Record(followUp.TargetKind, followUp.TargetId, ActivityType.QuickAction,
            $"Contacto con {WaitingOnText(followUp)}.", followUp.ProjectId, note: note,
            quickAction: QuickActionKind.InfoSent);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SetNextFollowUpAsync(Guid followUpId, DateTime nextUtc, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        followUp.NextFollowUpUtc = nextUtc;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> EscalateAsync(Guid followUpId, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        if (followUp.State == FollowUpState.Waiting)
        {
            followUp.State = FollowUpState.Escalated;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> ResolveAsync(ResolveFollowUpRequest request, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        var target = request.State is FollowUpState.Closed ? FollowUpState.Closed : FollowUpState.Answered;
        followUp.State = target;
        followUp.ResolvedAtUtc = clock.UtcNow;
        followUp.Resolution = Clean(request.Resolution);
        followUp.NextFollowUpUtc = null;

        activity.Record(followUp.TargetKind, followUp.TargetId, ActivityType.FollowUpResolved,
            target is FollowUpState.Closed
                ? $"Seguimiento cerrado: {followUp.Subject}."
                : $"Respuesta recibida de {WaitingOnText(followUp)}: {followUp.Subject}.",
            followUp.ProjectId, note: followUp.Resolution);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReopenAsync(Guid followUpId, CancellationToken cancellationToken = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, cancellationToken);
        if (followUp is null)
        {
            return NotFound();
        }

        followUp.State = FollowUpState.Waiting;
        followUp.ResolvedAtUtc = null;
        followUp.NextFollowUpUtc = clock.UtcNow.Add(DefaultNudgeInterval);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Guid?> ResolveProjectIdAsync(EntityKind kind, Guid id, CancellationToken cancellationToken) => kind switch
    {
        EntityKind.Project => id,
        EntityKind.WorkTask => await db.WorkTasks
            .Where(t => t.Id == id)
            .Select(t => (Guid?)t.ProjectId)
            .FirstOrDefaultAsync(cancellationToken),
        _ => null,
    };

    private static string WaitingOnText(FollowUp followUp)
        => followUp.WaitingOnLabel ?? "el destinatario";

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result NotFound() => Result.Failure("followup.not_found", "Seguimiento no encontrado.");
}
