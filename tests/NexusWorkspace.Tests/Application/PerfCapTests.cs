using FluentAssertions;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class PerfCapTests
{
    [Fact]
    public async Task Open_task_summary_counts_without_materialising_rows()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        var critical = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Crit", Priority = Priority.Critical });
        var overdue = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Late" });
        var waiting = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Wait" });
        var done = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Done" });

        await h.Tasks.UpdateDetailsAsync(new UpdateWorkTaskDetailsRequest { Id = overdue.Value, DueDateUtc = h.Clock.UtcNow.AddDays(-3) });
        await h.Tasks.ChangeStatusAsync(waiting.Value, WorkTaskStatus.WaitingClient);
        await h.Tasks.ChangeStatusAsync(done.Value, WorkTaskStatus.InProgress);
        await h.Tasks.ChangeStatusAsync(done.Value, WorkTaskStatus.Finished);

        var summary = await h.TaskReads.GetOpenSummaryAsync();
        summary.Open.Should().Be(3);
        summary.Critical.Should().Be(1);
        summary.Overdue.Should().Be(1);
        summary.Waiting.Should().Be(1);
    }

    [Fact]
    public async Task Workspace_wide_open_task_query_is_bounded()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });
        for (var i = 0; i < 25; i++)
        {
            await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = $"T{i}" });
        }

        var rows = await h.TaskReads.GetOpenAcrossWorkspaceAsync();
        rows.Should().HaveCount(25);
        rows.Count.Should().BeLessThanOrEqualTo(200, "the workspace-wide widget query caps its result set");
    }
}
