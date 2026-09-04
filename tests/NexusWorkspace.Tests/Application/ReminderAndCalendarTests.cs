using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class ReminderAndCalendarTests
{
    [Fact]
    public async Task Due_unnotified_query_drives_the_scheduler()
    {
        await using var h = new TestHarness();

        var soon = await h.Reminders.CreateAsync(new CreateReminderRequest
        {
            Text = "Llamar a Igor", RemindAtUtc = h.Clock.UtcNow.AddMinutes(-5),
        });
        await h.Reminders.CreateAsync(new CreateReminderRequest
        {
            Text = "Más tarde", RemindAtUtc = h.Clock.UtcNow.AddDays(2),
        });

        var reads = new ReminderReadService(h.Db);

        var due = await reads.GetDueUnnotifiedAsync(h.Clock.UtcNow);
        due.Should().ContainSingle().Which.Id.Should().Be(soon.Value);

        await h.Reminders.MarkNotifiedAsync(soon.Value);
        (await reads.GetDueUnnotifiedAsync(h.Clock.UtcNow)).Should().BeEmpty();
        (await reads.CountPendingAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Completing_a_reminder_removes_it_from_pending()
    {
        await using var h = new TestHarness();
        var r = await h.Reminders.CreateAsync(new CreateReminderRequest
        {
            Text = "algo", RemindAtUtc = h.Clock.UtcNow.AddHours(1),
        });

        await h.Reminders.CompleteAsync(r.Value);

        var reminder = await h.Db.Reminders.SingleAsync();
        reminder.Status.Should().Be(ReminderStatus.Done);
        reminder.CompletedAtUtc.Should().Be(h.Clock.UtcNow);
        (await new ReminderReadService(h.Db).CountPendingAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Calendar_aggregates_task_due_dates_and_reminders()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Kioskos" });
        var due = h.Clock.UtcNow.AddDays(3);

        var task = await h.Tasks.CreateAsync(new CreateWorkTaskRequest
        {
            ProjectId = project.Value, Title = "Preparar operativo", DueDateUtc = due,
        });
        await h.Reminders.CreateAsync(new CreateReminderRequest { Text = "Confirmar envío", RemindAtUtc = due.AddDays(1) });

        var calendar = new CalendarReadService(h.Db);
        var entries = await calendar.GetRangeAsync(h.Clock.UtcNow.AddDays(-1), h.Clock.UtcNow.AddDays(30));

        entries.Should().Contain(e => e.Kind == CalendarEntryKind.TaskDue && e.NavigateId == task.Value);
        entries.Should().Contain(e => e.Kind == CalendarEntryKind.Reminder);
    }
}
