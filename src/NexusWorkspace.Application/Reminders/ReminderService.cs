using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Reminders;

namespace NexusWorkspace.Application.Reminders;

public sealed class ReminderService(IApplicationDbContext db, IClock clock, IActivityLog activity)
{
    public async Task<Result<Guid>> CreateAsync(CreateReminderRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<Guid>("reminder.text_required", "Escribe el texto del recordatorio.");
        }

        if (request.RemindAtUtc == default)
        {
            return Result.Failure<Guid>("reminder.date_required", "Indica cuándo recordar.");
        }

        var projectId = await ResolveProjectIdAsync(request.TargetKind, request.TargetId, cancellationToken);

        var reminder = new Reminder
        {
            Text = text,
            RemindAtUtc = request.RemindAtUtc,
            Status = ReminderStatus.Pending,
            TargetKind = request.TargetKind,
            TargetId = request.TargetId,
            ProjectId = projectId,
        };

        db.Reminders.Add(reminder);

        if (request.TargetKind is { } kind && request.TargetId is { } targetId)
        {
            activity.Record(kind, targetId, ActivityType.Updated,
                $"Recordatorio para el {request.RemindAtUtc:dd/MM/yyyy}: {text}.", projectId);
        }

        await db.SaveChangesAsync(cancellationToken);
        return reminder.Id;
    }

    public Task<Result> CompleteAsync(Guid reminderId, CancellationToken cancellationToken = default)
        => SetStatusAsync(reminderId, ReminderStatus.Done, cancellationToken);

    public Task<Result> DismissAsync(Guid reminderId, CancellationToken cancellationToken = default)
        => SetStatusAsync(reminderId, ReminderStatus.Dismissed, cancellationToken);

    public async Task<Result> SnoozeAsync(Guid reminderId, TimeSpan by, CancellationToken cancellationToken = default)
    {
        var reminder = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (reminder is null)
        {
            return NotFound();
        }

        reminder.RemindAtUtc = (reminder.RemindAtUtc < clock.UtcNow ? clock.UtcNow : reminder.RemindAtUtc).Add(by);
        reminder.Status = ReminderStatus.Pending;
        reminder.Notified = false;
        reminder.CompletedAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RescheduleAsync(Guid reminderId, DateTime newRemindAtUtc, CancellationToken cancellationToken = default)
    {
        var reminder = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (reminder is null)
        {
            return NotFound();
        }

        reminder.RemindAtUtc = newRemindAtUtc;
        reminder.Status = ReminderStatus.Pending;
        reminder.Notified = false;
        reminder.CompletedAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Used by the scheduler once it has raised a notification for a reminder.</summary>
    public async Task MarkNotifiedAsync(Guid reminderId, CancellationToken cancellationToken = default)
    {
        var reminder = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (reminder is not null && !reminder.Notified)
        {
            reminder.Notified = true;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Result> SetStatusAsync(Guid reminderId, ReminderStatus status, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (reminder is null)
        {
            return NotFound();
        }

        reminder.Status = status;
        reminder.CompletedAtUtc = status == ReminderStatus.Done ? clock.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Guid?> ResolveProjectIdAsync(EntityKind? kind, Guid? id, CancellationToken cancellationToken)
    {
        if (kind is null || id is null)
        {
            return null;
        }

        return kind switch
        {
            EntityKind.Project => id,
            EntityKind.WorkTask => await db.WorkTasks
                .Where(t => t.Id == id)
                .Select(t => (Guid?)t.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            _ => null,
        };
    }

    private static Result NotFound() => Result.Failure("reminder.not_found", "Recordatorio no encontrado.");
}
