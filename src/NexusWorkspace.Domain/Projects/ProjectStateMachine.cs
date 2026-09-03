using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Projects;

/// <summary>Allowed <see cref="ProjectStatus"/> transitions.</summary>
public static class ProjectStateMachine
{
    private static readonly IReadOnlyDictionary<ProjectStatus, ProjectStatus[]> Transitions =
        new Dictionary<ProjectStatus, ProjectStatus[]>
        {
            [ProjectStatus.Planning] = [ProjectStatus.Active, ProjectStatus.OnHold, ProjectStatus.Blocked, ProjectStatus.Finished],
            [ProjectStatus.Active] = [ProjectStatus.Planning, ProjectStatus.OnHold, ProjectStatus.Blocked, ProjectStatus.Finished],
            [ProjectStatus.OnHold] = [ProjectStatus.Active, ProjectStatus.Blocked, ProjectStatus.Finished],
            [ProjectStatus.Blocked] = [ProjectStatus.Active, ProjectStatus.OnHold, ProjectStatus.Finished],
            [ProjectStatus.Finished] = [ProjectStatus.Active],
        };

    public static bool CanTransition(ProjectStatus from, ProjectStatus to)
        => from == to
           || (Transitions.TryGetValue(from, out var allowed) && Array.IndexOf(allowed, to) >= 0);

    public static IReadOnlyList<ProjectStatus> NextStates(ProjectStatus from)
        => Transitions.TryGetValue(from, out var allowed) ? allowed : [];

    public static void EnsureCanTransition(ProjectStatus from, ProjectStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(nameof(Project), from, to);
        }
    }
}
