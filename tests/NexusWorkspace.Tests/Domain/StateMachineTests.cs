using FluentAssertions;
using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;
using Xunit;

namespace NexusWorkspace.Tests.Domain;

public class StateMachineTests
{
    [Theory]
    [InlineData(WorkTaskStatus.Pending, WorkTaskStatus.InProgress, true)]
    [InlineData(WorkTaskStatus.Pending, WorkTaskStatus.WaitingProvider, true)]
    [InlineData(WorkTaskStatus.Pending, WorkTaskStatus.Finished, false)]
    [InlineData(WorkTaskStatus.InProgress, WorkTaskStatus.Finished, true)]
    [InlineData(WorkTaskStatus.Finished, WorkTaskStatus.InProgress, true)]
    [InlineData(WorkTaskStatus.Finished, WorkTaskStatus.WaitingClient, false)]
    [InlineData(WorkTaskStatus.Cancelled, WorkTaskStatus.Pending, true)]
    [InlineData(WorkTaskStatus.Cancelled, WorkTaskStatus.InProgress, false)]
    public void WorkTask_transitions_match_the_matrix(WorkTaskStatus from, WorkTaskStatus to, bool allowed)
    {
        WorkTaskStateMachine.CanTransition(from, to).Should().Be(allowed);
    }

    [Fact]
    public void WorkTask_same_status_is_always_allowed()
    {
        foreach (var status in Enum.GetValues<WorkTaskStatus>())
        {
            WorkTaskStateMachine.CanTransition(status, status).Should().BeTrue();
        }
    }

    [Fact]
    public void WorkTask_EnsureCanTransition_throws_on_invalid()
    {
        var act = () => WorkTaskStateMachine.EnsureCanTransition(WorkTaskStatus.Pending, WorkTaskStatus.Finished);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Theory]
    [InlineData(QuickActionKind.PendingProvider, WorkTaskStatus.WaitingProvider)]
    [InlineData(QuickActionKind.PendingClient, WorkTaskStatus.WaitingClient)]
    [InlineData(QuickActionKind.EmailSent, null)]
    public void QuickAction_status_mapping(QuickActionKind action, WorkTaskStatus? expected)
    {
        WorkTaskStateMachine.StatusFor(action).Should().Be(expected);
    }

    [Theory]
    [InlineData(ProjectStatus.Planning, ProjectStatus.Active, true)]
    [InlineData(ProjectStatus.Active, ProjectStatus.Finished, true)]
    [InlineData(ProjectStatus.Finished, ProjectStatus.OnHold, false)]
    [InlineData(ProjectStatus.Finished, ProjectStatus.Active, true)]
    public void Project_transitions_match_the_matrix(ProjectStatus from, ProjectStatus to, bool allowed)
    {
        ProjectStateMachine.CanTransition(from, to).Should().Be(allowed);
    }
}
