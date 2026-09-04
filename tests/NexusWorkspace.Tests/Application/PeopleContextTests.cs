using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.Meetings;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.SavedSearches;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class PeopleContextTests
{
    [Fact]
    public async Task Person_detail_aggregates_projects_tasks_and_communications()
    {
        await using var h = new TestHarness();

        var company = await h.Companies.CreateAsync(new CreateCompanyRequest { Name = "Axon", Kind = CompanyKind.Provider });
        var person = await h.People.CreateAsync(new CreatePersonRequest { Name = "Igor Ríos", CompanyId = company.Value });
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Integración SIP" });
        var task = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Endpoint de PRE" });

        // Link the person to the project and assign a task.
        h.Db.ProjectPeople.Add(new ProjectPerson { PersonId = person.Value, ProjectId = project.Value, LinkedAtUtc = h.Clock.UtcNow });
        var entity = await h.Db.WorkTasks.SingleAsync(t => t.Id == task.Value);
        entity.AssigneePersonId = person.Value;
        await h.Db.SaveChangesAsync();

        var log = await h.Communications.LogAsync(new LogCommunicationRequest
        {
            Channel = CommunicationChannel.Email,
            Direction = CommunicationDirection.Outbound,
            Subject = "Recordatorio endpoint PRE",
            PersonId = person.Value,
            ProjectId = project.Value,
        });
        log.IsSuccess.Should().BeTrue();

        var detail = await h.PeopleReads.GetDetailAsync(person.Value);
        detail.Should().NotBeNull();
        detail!.CompanyName.Should().Be("Axon");
        detail.ProjectCount.Should().Be(1);
        detail.OpenTaskCount.Should().Be(1);
        detail.CommunicationCount.Should().Be(1);

        var feed = await h.CommunicationReads.GetForPersonAsync(person.Value);
        feed.Should().ContainSingle().Which.Subject.Should().Be("Recordatorio endpoint PRE");

        // Logging a communication refreshes "last contacted" and appends person history.
        (await h.Db.People.SingleAsync(p => p.Id == person.Value)).LastContactedUtc.Should().Be(h.Clock.UtcNow);
        (await h.Db.ActivityEvents.CountAsync(e => e.TargetKind == EntityKind.Person && e.Type == ActivityType.CommunicationLogged))
            .Should().Be(1);
    }

    [Fact]
    public async Task Tag_filter_narrows_people_and_reports_usage_counts()
    {
        await using var h = new TestHarness();

        var tag = await h.Tags.CreateAsync(new CreateTagRequest { Name = "SIP" });
        var a = await h.People.CreateAsync(new CreatePersonRequest { Name = "Con etiqueta" });
        await h.People.CreateAsync(new CreatePersonRequest { Name = "Sin etiqueta" });
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        (await h.People.AddTagAsync(a.Value, tag.Value)).IsSuccess.Should().BeTrue();
        h.Db.ProjectTags.Add(new ProjectTag { ProjectId = project.Value, TagId = tag.Value });
        await h.Db.SaveChangesAsync();

        var filtered = await h.PeopleReads.GetListAsync(PersonScope.Active, null, tag.Value);
        filtered.Should().ContainSingle().Which.Name.Should().Be("Con etiqueta");

        var all = await h.PeopleReads.GetListAsync(PersonScope.Active);
        all.Should().HaveCount(2);

        var tags = await h.TagReads.GetAllAsync();
        var sip = tags.Single(t => t.Name == "SIP");
        sip.PersonCount.Should().Be(1);
        sip.ProjectCount.Should().Be(1);
        sip.TotalUses.Should().Be(2);
    }

    [Fact]
    public async Task Meeting_shows_up_in_calendar_range_and_person_feed()
    {
        await using var h = new TestHarness();

        var person = await h.People.CreateAsync(new CreatePersonRequest { Name = "Marta" });
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });
        var start = h.Clock.UtcNow.AddDays(2);

        var meeting = await h.Meetings.ScheduleAsync(new ScheduleMeetingRequest
        {
            Title = "Seguimiento",
            StartUtc = start,
            EndUtc = start.AddHours(1),
            ProjectId = project.Value,
            Participants = [new MeetingParticipantInput { PersonId = person.Value }],
        });
        meeting.IsSuccess.Should().BeTrue();

        var forPerson = await h.MeetingReads.GetForPersonAsync(person.Value);
        forPerson.Should().ContainSingle().Which.Title.Should().Be("Seguimiento");

        var calendar = new NexusWorkspace.Application.Calendar.CalendarReadService(h.Db);
        var range = await calendar.GetRangeAsync(h.Clock.UtcNow, h.Clock.UtcNow.AddDays(7));
        range.Should().Contain(e => e.Kind == NexusWorkspace.Application.Calendar.CalendarEntryKind.Meeting && e.Title == "Seguimiento");
    }

    [Fact]
    public async Task Relation_is_visible_from_both_endpoints_and_can_be_removed()
    {
        await using var h = new TestHarness();

        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Proyecto A" });
        var person = await h.People.CreateAsync(new CreatePersonRequest { Name = "Igor" });

        var add = await h.Relations.AddAsync(new AddRelationRequest
        {
            FromKind = EntityKind.Person,
            FromId = person.Value,
            ToKind = EntityKind.Project,
            ToId = project.Value,
            Kind = RelationKind.RelatesTo,
        });
        add.IsSuccess.Should().BeTrue();

        var fromPerson = await h.RelationReads.GetForEntityAsync(EntityKind.Person, person.Value);
        fromPerson.Should().ContainSingle().Which.OtherLabel.Should().Be("Proyecto A");

        var fromProject = await h.RelationReads.GetForEntityAsync(EntityKind.Project, project.Value);
        fromProject.Should().ContainSingle().Which.OtherLabel.Should().Be("Igor");

        // Adding the mirror pair is rejected as a duplicate.
        var dup = await h.Relations.AddAsync(new AddRelationRequest
        {
            FromKind = EntityKind.Project,
            FromId = project.Value,
            ToKind = EntityKind.Person,
            ToId = person.Value,
            Kind = RelationKind.RelatesTo,
        });
        dup.IsFailure.Should().BeTrue();

        await h.Relations.RemoveAsync(add.Value);
        (await h.RelationReads.GetForEntityAsync(EntityKind.Person, person.Value)).Should().BeEmpty();
    }

    [Fact]
    public async Task Saved_search_round_trips_by_kind_and_pins()
    {
        await using var h = new TestHarness();

        var saved = await h.SavedSearches.SaveAsync(new SaveSearchRequest
        {
            Name = "Todo lo de Axon",
            Kind = SavedSearchKind.People,
            QueryText = "axon",
            IsPinned = true,
        });
        saved.IsSuccess.Should().BeTrue();

        var byKind = await h.SavedSearchReads.GetByKindAsync(SavedSearchKind.People);
        byKind.Should().ContainSingle().Which.QueryText.Should().Be("axon");

        (await h.SavedSearchReads.GetByKindAsync(SavedSearchKind.Projects)).Should().BeEmpty();

        // Saving the same name/kind updates in place rather than duplicating.
        await h.SavedSearches.SaveAsync(new SaveSearchRequest
        {
            Name = "Todo lo de Axon",
            Kind = SavedSearchKind.People,
            QueryText = "axon sip",
        });
        var again = await h.SavedSearchReads.GetByKindAsync(SavedSearchKind.People);
        again.Should().ContainSingle().Which.QueryText.Should().Be("axon sip");
    }

    [Fact]
    public async Task Company_and_person_become_searchable_in_the_global_index()
    {
        await using var h = new TestHarness();

        await h.Companies.CreateAsync(new CreateCompanyRequest { Name = "Axon Telecom" });
        await h.People.CreateAsync(new CreatePersonRequest { Name = "Igor Ríos", Role = "Contacto SIP" });

        var hits = await h.Search.SearchAsync("axon");
        hits.Should().Contain(x => x.EntityKind == EntityKind.Company);

        var people = await h.Search.SearchAsync("igor");
        people.Should().Contain(x => x.EntityKind == EntityKind.Person);
    }
}
