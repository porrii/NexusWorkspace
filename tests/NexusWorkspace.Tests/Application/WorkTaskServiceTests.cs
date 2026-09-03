using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class WorkTaskServiceTests
{
    private static async Task<Guid> NewProjectAsync(TestHarness harness)
    {
        var result = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Bideogune V2" });
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task Creating_a_task_persists_it_and_writes_a_created_event()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);

        var result = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest
        {
            ProjectId = projectId,
            Title = "Integración con SIP",
            Priority = Priority.High,
        });

        result.IsSuccess.Should().BeTrue();

        var task = await harness.Db.WorkTasks.SingleAsync();
        task.Title.Should().Be("Integración con SIP");
        task.Status.Should().Be(WorkTaskStatus.Pending);
        task.CreatedAtUtc.Should().Be(harness.Clock.UtcNow);

        var events = await harness.Db.ActivityEvents.Where(e => e.TargetId == task.Id).ToListAsync();
        events.Should().ContainSingle(e => e.Type == ActivityType.Created);
    }

    [Fact]
    public async Task Invalid_status_transition_is_rejected_without_changing_state()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);
        var created = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = projectId, Title = "X" });

        var result = await harness.Tasks.ChangeStatusAsync(created.Value, WorkTaskStatus.Finished);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("task.invalid_transition");

        var task = await harness.Db.WorkTasks.SingleAsync();
        task.Status.Should().Be(WorkTaskStatus.Pending);
    }

    [Fact]
    public async Task Valid_status_transition_records_old_and_new_values()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);
        var created = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = projectId, Title = "X" });

        var result = await harness.Tasks.ChangeStatusAsync(created.Value, WorkTaskStatus.InProgress);

        result.IsSuccess.Should().BeTrue();

        var evt = await harness.Db.ActivityEvents
            .Where(e => e.Type == ActivityType.StatusChanged)
            .SingleAsync();
        evt.OldValue.Should().Be(nameof(WorkTaskStatus.Pending));
        evt.NewValue.Should().Be(nameof(WorkTaskStatus.InProgress));

        var task = await harness.Db.WorkTasks.SingleAsync();
        task.StartedAtUtc.Should().Be(harness.Clock.UtcNow);
    }

    [Fact]
    public async Task Quick_action_PendingProvider_pushes_status_and_logs_both_events()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);
        var created = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = projectId, Title = "X" });

        var result = await harness.Tasks.ExecuteQuickActionAsync(created.Value, QuickActionKind.PendingProvider, "a la espera de ETIQMEDIA");

        result.IsSuccess.Should().BeTrue();

        var task = await harness.Db.WorkTasks.SingleAsync();
        task.Status.Should().Be(WorkTaskStatus.WaitingProvider);

        var types = await harness.Db.ActivityEvents.Select(e => e.Type).ToListAsync();
        types.Should().Contain(ActivityType.QuickAction);
        types.Should().Contain(ActivityType.StatusChanged);
    }

    [Fact]
    public async Task Subtask_progress_is_reflected_in_the_read_model()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);
        var created = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = projectId, Title = "X" });

        var a = await harness.Tasks.AddSubTaskAsync(created.Value, "Revisar API");
        await harness.Tasks.AddSubTaskAsync(created.Value, "Implementar");
        await harness.Tasks.SetSubTaskDoneAsync(a.Value, true);

        var reads = new WorkTaskReadService(harness.Db);
        var detail = await reads.GetDetailAsync(created.Value);

        detail.Should().NotBeNull();
        detail!.SubTasks.Should().HaveCount(2);
        detail.SubTasks.Count(s => s.IsDone).Should().Be(1);
    }

    [Fact]
    public async Task Trashing_a_task_hides_it_from_default_queries_but_keeps_history()
    {
        await using var harness = new TestHarness();
        var projectId = await NewProjectAsync(harness);
        var created = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = projectId, Title = "X" });

        await harness.Tasks.TrashAsync(created.Value);

        (await harness.Db.WorkTasks.CountAsync()).Should().Be(0);
        (await harness.Db.WorkTasks.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await harness.Db.ActivityEvents.CountAsync(e => e.TargetId == created.Value)).Should().BeGreaterThan(0);
    }
}
