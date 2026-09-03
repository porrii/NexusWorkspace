using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Inbox;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class InboxServiceTests
{
    [Fact]
    public async Task Capture_creates_a_pending_item()
    {
        await using var harness = new TestHarness();

        var id = await harness.Inbox.CaptureAsync("Mirar versión de OpenJDK de Axon", "Proyecto: Axon");

        var reads = new InboxReadService(harness.Db);
        var pending = await reads.GetPendingAsync();

        pending.Should().ContainSingle(i => i.Id == id);
        pending[0].ParsedHint.Should().Be("Proyecto: Axon");
        (await reads.CountPendingAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Convert_to_task_creates_the_task_and_marks_the_item()
    {
        await using var harness = new TestHarness();
        var project = await harness.Projects.CreateAsync(new CreateProjectRequest { Name = "Axon" });
        var inboxId = await harness.Inbox.CaptureAsync("Desregistrar incidencia", null);

        var result = await harness.Inbox.ConvertToTaskAsync(inboxId, project.Value, new ConvertToTaskOptions
        {
            Priority = Priority.High,
        });

        result.IsSuccess.Should().BeTrue();

        var task = await harness.Db.WorkTasks.SingleAsync();
        task.Title.Should().Be("Desregistrar incidencia");
        task.Priority.Should().Be(Priority.High);

        var item = await harness.Db.InboxItems.IgnoreQueryFilters().SingleAsync(i => i.Id == inboxId);
        item.State.Should().Be(InboxItemState.Converted);
        item.ConvertedToKind.Should().Be(EntityKind.WorkTask);
        item.ConvertedToId.Should().Be(task.Id);

        var events = await harness.Db.ActivityEvents.Where(e => e.TargetId == task.Id).ToListAsync();
        events.Should().Contain(e => e.Type == ActivityType.Created && e.Note == "desde Inbox");

        (await new InboxReadService(harness.Db).CountPendingAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Dismiss_soft_deletes_the_item()
    {
        await using var harness = new TestHarness();
        var inboxId = await harness.Inbox.CaptureAsync("algo", null);

        await harness.Inbox.DismissAsync(inboxId);

        (await harness.Db.InboxItems.CountAsync()).Should().Be(0);
        var item = await harness.Db.InboxItems.IgnoreQueryFilters().SingleAsync();
        item.State.Should().Be(InboxItemState.Dismissed);
        item.IsDeleted.Should().BeTrue();
    }
}
