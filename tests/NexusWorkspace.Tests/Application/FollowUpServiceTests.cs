using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class FollowUpServiceTests
{
    private static async Task<(Guid ProjectId, Guid TaskId)> SeedAsync(TestHarness h)
    {
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Bideogune V2" });
        var task = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Integración con SIP" });
        return (project.Value, task.Value);
    }

    [Fact]
    public async Task Start_records_project_and_history_and_shows_days_waiting()
    {
        await using var h = new TestHarness();
        var (projectId, taskId) = await SeedAsync(h);

        var result = await h.FollowUps.StartAsync(new StartFollowUpRequest
        {
            TargetKind = EntityKind.WorkTask,
            TargetId = taskId,
            Subject = "Confirmación del endpoint de PRE",
            WaitingOnLabel = "ETIQMEDIA",
        });

        result.IsSuccess.Should().BeTrue();

        var followUp = await h.Db.FollowUps.SingleAsync();
        followUp.ProjectId.Should().Be(projectId);
        followUp.State.Should().Be(FollowUpState.Waiting);
        followUp.WaitingOnLabel.Should().Be("ETIQMEDIA");

        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.FollowUpStarted)).Should().Be(1);

        h.Clock.Advance(TimeSpan.FromDays(9));
        var list = await new FollowUpReadService(h.Db, h.Clock).GetListAsync(FollowUpScope.Open);
        list.Should().ContainSingle().Which.DaysWaiting.Should().Be(9);
    }

    [Fact]
    public async Task SendReminder_increments_count_and_logs()
    {
        await using var h = new TestHarness();
        var (_, taskId) = await SeedAsync(h);
        var fu = await h.FollowUps.StartAsync(new StartFollowUpRequest
        {
            TargetKind = EntityKind.WorkTask, TargetId = taskId, Subject = "algo", WaitingOnLabel = "Axon",
        });

        await h.FollowUps.SendReminderAsync(fu.Value, "reenviado el correo");
        await h.FollowUps.SendReminderAsync(fu.Value);

        var followUp = await h.Db.FollowUps.SingleAsync();
        followUp.ReminderCount.Should().Be(2);
        followUp.LastContactUtc.Should().Be(h.Clock.UtcNow);

        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.FollowUpReminderSent)).Should().Be(2);
    }

    [Fact]
    public async Task Resolve_marks_answered_and_leaves_open_list()
    {
        await using var h = new TestHarness();
        var (_, taskId) = await SeedAsync(h);
        var fu = await h.FollowUps.StartAsync(new StartFollowUpRequest
        {
            TargetKind = EntityKind.WorkTask, TargetId = taskId, Subject = "algo", WaitingOnLabel = "Igor",
        });

        await h.FollowUps.ResolveAsync(new ResolveFollowUpRequest { Id = fu.Value, State = FollowUpState.Answered });

        var followUp = await h.Db.FollowUps.SingleAsync();
        followUp.State.Should().Be(FollowUpState.Answered);
        followUp.ResolvedAtUtc.Should().Be(h.Clock.UtcNow);

        var reads = new FollowUpReadService(h.Db, h.Clock);
        (await reads.GetListAsync(FollowUpScope.Open)).Should().BeEmpty();
        (await reads.CountOpenAsync()).Should().Be(0);
        (await reads.GetListAsync(FollowUpScope.All)).Should().HaveCount(1);
    }
}
