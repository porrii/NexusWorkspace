namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// In-process background loop that fires due reminders while the app is running.
/// No internet, no OS service — just a timer over the local database.
/// </summary>
public interface ISchedulerService
{
    void Start();

    void Stop();
}
