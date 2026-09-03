using FluentAssertions;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Infrastructure;

public class Fts5SearchServiceTests
{
    [Fact]
    public async Task Finds_a_task_by_title_accent_insensitively()
    {
        await using var harness = new TestHarness();
        var project = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Bideogune V2" });
        var task = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest
        {
            ProjectId = project.Value,
            Title = "Integración con SIP",
        });

        var hits = await harness.Search.SearchAsync("integracion");

        hits.Should().Contain(h => h.NavigateKind == EntityKind.WorkTask && h.NavigateId == task.Value);
    }

    [Fact]
    public async Task Finds_a_task_by_comment_body_and_navigates_to_the_task()
    {
        await using var harness = new TestHarness();
        var project = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Bideogune V2" });
        var task = await harness.Tasks.CreateAsync(new CreateWorkTaskRequest
        {
            ProjectId = project.Value,
            Title = "Validación de conservación",
        });
        await harness.Tasks.AddCommentAsync(task.Value, "Problema detectado al poner conservar desde SIP.");

        var hits = await harness.Search.SearchAsync("conservar");

        hits.Should().Contain(h => h.EntityKind == EntityKind.Comment
            && h.NavigateKind == EntityKind.WorkTask
            && h.NavigateId == task.Value);
    }

    [Fact]
    public async Task Rebuild_repopulates_from_scratch()
    {
        await using var harness = new TestHarness();
        var project = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Kioskos" });
        await harness.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Preparar operativo" });

        await harness.Search.RebuildAsync();

        (await harness.Search.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
        var hits = await harness.Search.SearchAsync("operativo");
        hits.Should().NotBeEmpty();
    }
}
