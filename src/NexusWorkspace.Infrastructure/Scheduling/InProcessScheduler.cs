using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Infrastructure.Scheduling;

/// <summary>
/// A single background loop that, every ~45 s while the app runs, raises a
/// notification for each pending reminder that has come due. No internet, no OS
/// service — just a timer over the local database.
/// </summary>
public sealed class InProcessScheduler(
    IUnitOfWorkRunner runner,
    IClock clock,
    ILogger<InProcessScheduler> logger) : ISchedulerService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(45);

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunLoopAsync(_cts.Token));
        logger.LogInformation("Scheduler de recordatorios iniciado.");
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _cts = null;
        _loop = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        await TickAsync(cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Stop() was called.
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            await runner.RunAsync(async (sp, token) =>
            {
                var reads = sp.GetRequiredService<ReminderReadService>();
                var reminders = sp.GetRequiredService<ReminderService>();
                var notifications = sp.GetRequiredService<INotificationService>();

                var due = await reads.GetDueUnnotifiedAsync(clock.UtcNow, token).ConfigureAwait(false);
                foreach (var reminder in due)
                {
                    await notifications.PublishAsync(
                        NotificationKind.Reminder,
                        "Recordatorio",
                        reminder.Text,
                        reminder.TargetKind,
                        reminder.TargetId,
                        reminder.ProjectId,
                        token).ConfigureAwait(false);

                    await reminders.MarkNotifiedAsync(reminder.Id, token).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallo en un ciclo del scheduler de recordatorios.");
        }
    }
}
