using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_requires_a_name()
    {
        await using var harness = new TestHarness();

        var result = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "   " });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("project.name_required");
        (await harness.Db.Projects.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ChangeStatusAsync_finishing_sets_completed_date_and_logs()
    {
        await using var harness = new TestHarness();
        harness.Settings.Current.DefaultProjectStatus = ProjectStatus.Planning;
        var created = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Axon" });

        await harness.Projects.ChangeStatusAsync(created.Value, ProjectStatus.Active);
        harness.Clock.Advance(TimeSpan.FromHours(3));
        var result = await harness.Projects.ChangeStatusAsync(created.Value, ProjectStatus.Finished);

        result.IsSuccess.Should().BeTrue();

        var project = await harness.Db.Projects.SingleAsync();
        project.Status.Should().Be(ProjectStatus.Finished);
        project.CompletedDateUtc.Should().Be(harness.Clock.UtcNow);

        (await harness.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.StatusChanged))
            .Should().Be(2);
    }

    [Fact]
    public async Task Archiving_then_listing_respects_scope()
    {
        await using var harness = new TestHarness();
        var keep = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Activo" });
        var archive = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Viejo" });
        await harness.Projects.ArchiveAsync(archive.Value);

        var reads = new ProjectReadService(harness.Db);

        (await reads.GetListAsync(ProjectListScope.Active)).Should().ContainSingle(p => p.Id == keep.Value);
        (await reads.GetListAsync(ProjectListScope.Archived)).Should().ContainSingle(p => p.Id == archive.Value);
        (await reads.GetListAsync(ProjectListScope.All)).Should().HaveCount(2);
    }
}
