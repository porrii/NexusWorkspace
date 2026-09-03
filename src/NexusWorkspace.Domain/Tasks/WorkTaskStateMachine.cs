using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Tasks;

/// <summary>
/// Allowed <see cref="WorkTaskStatus"/> transitions. A transition to the same
/// status is always allowed (idempotent). Every real transition is expected to
/// produce a <c>StatusChanged</c> activity event in the application layer.
/// </summary>
public static class WorkTaskStateMachine
{
    private static readonly IReadOnlyDictionary<WorkTaskStatus, WorkTaskStatus[]> Transitions =
        new Dictionary<WorkTaskStatus, WorkTaskStatus[]>
        {
            [WorkTaskStatus.Pending] =
            [
                WorkTaskStatus.InProgress, WorkTaskStatus.WaitingClient, WorkTaskStatus.WaitingProvider,
                WorkTaskStatus.Blocked, WorkTaskStatus.Cancelled,
            ],
            [WorkTaskStatus.InProgress] =
            [
                WorkTaskStatus.Pending, WorkTaskStatus.WaitingClient, WorkTaskStatus.WaitingProvider,
                WorkTaskStatus.Blocked, WorkTaskStatus.Finished, WorkTaskStatus.Cancelled,
            ],
            [WorkTaskStatus.WaitingClient] =
            [
                WorkTaskStatus.InProgress, WorkTaskStatus.WaitingProvider, WorkTaskStatus.Blocked,
                WorkTaskStatus.Finished, WorkTaskStatus.Cancelled,
            ],
            [WorkTaskStatus.WaitingProvider] =
            [
                WorkTaskStatus.InProgress, WorkTaskStatus.WaitingClient, WorkTaskStatus.Blocked,
                WorkTaskStatus.Finished, WorkTaskStatus.Cancelled,
            ],
            [WorkTaskStatus.Blocked] =
            [
                WorkTaskStatus.InProgress, WorkTaskStatus.WaitingClient, WorkTaskStatus.WaitingProvider,
                WorkTaskStatus.Cancelled,
            ],
            [WorkTaskStatus.Finished] = [WorkTaskStatus.InProgress],
            [WorkTaskStatus.Cancelled] = [WorkTaskStatus.Pending],
        };

    public static bool CanTransition(WorkTaskStatus from, WorkTaskStatus to)
        => from == to
           || (Transitions.TryGetValue(from, out var allowed) && Array.IndexOf(allowed, to) >= 0);

    public static IReadOnlyList<WorkTaskStatus> NextStates(WorkTaskStatus from)
        => Transitions.TryGetValue(from, out var allowed) ? allowed : [];

    public static void EnsureCanTransition(WorkTaskStatus from, WorkTaskStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(nameof(WorkTask), from, to);
        }
    }

    /// <summary>
    /// Maps a quick action to the status it should push the task into, when it has one.
    /// </summary>
    public static WorkTaskStatus? StatusFor(QuickActionKind action) => action switch
    {
        QuickActionKind.PendingClient => WorkTaskStatus.WaitingClient,
        QuickActionKind.PendingProvider => WorkTaskStatus.WaitingProvider,
        QuickActionKind.IncidentResolved => WorkTaskStatus.InProgress,
        _ => null,
    };
}
