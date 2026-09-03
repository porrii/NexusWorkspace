using FluentAssertions;
using NexusWorkspace.Application.QuickCapture;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.QuickCapture;

public class RuleBasedQuickCaptureParserTests
{
    // Thursday 2026-09-03 08:00 UTC
    private readonly FixedClock _clock = new(new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc));

    private RuleBasedQuickCaptureParser Parser => new(_clock);

    private static readonly ProjectNameRef[] KnownProjects =
    [
        new(Guid.NewGuid(), "Axon"),
        new(Guid.NewGuid(), "Bideogune V2"),
    ];

    [Fact]
    public void Detects_action_project_and_relative_date()
    {
        var result = Parser.Parse("Revisar Axon mañana", KnownProjects);

        result.Action.Should().Be("Revisar");
        result.ProjectName.Should().Be("Axon");
        result.ProjectId.Should().Be(KnownProjects[0].Id);
        result.DueDateUtc!.Value.Date.Should().Be(new DateTime(2026, 9, 4));
    }

    [Fact]
    public void Strips_tokens_and_reads_tags_people_priority()
    {
        var result = Parser.Parse("enviar informe #SIP @igor !alta", KnownProjects);

        result.Priority.Should().Be(Priority.High);
        result.Tags.Should().ContainSingle().Which.Should().Be("SIP");
        result.People.Should().ContainSingle().Which.Should().Be("igor");
        result.CleanText.Should().Be("enviar informe");
        result.Action.Should().Be("Enviar");
    }

    [Fact]
    public void Resolves_weekday_to_next_occurrence()
    {
        var result = Parser.Parse("llamar el lunes", KnownProjects);

        // Next Monday after Thu 2026-09-03 is 2026-09-07
        result.DueDateUtc!.Value.Date.Should().Be(new DateTime(2026, 9, 7));
    }

    [Fact]
    public void Parses_day_month_and_rolls_to_next_year_when_past()
    {
        var result = Parser.Parse("cerrar 01/02", KnownProjects);

        result.DueDateUtc!.Value.Date.Should().Be(new DateTime(2027, 2, 1));
    }

    [Fact]
    public void Without_known_projects_leaves_project_unset()
    {
        var result = Parser.Parse("Revisar Axon mañana");

        result.ProjectId.Should().BeNull();
        result.Action.Should().Be("Revisar");
        result.DueDateUtc.Should().NotBeNull();
    }

    [Fact]
    public void Diacritic_insensitive_project_match()
    {
        var projects = new[] { new ProjectNameRef(Guid.NewGuid(), "Cartografía") };
        var result = Parser.Parse("mirar cartografia", projects);

        result.ProjectId.Should().Be(projects[0].Id);
    }

    [Fact]
    public void Longest_project_name_wins()
    {
        var projects = new[]
        {
            new ProjectNameRef(Guid.NewGuid(), "Axon"),
            new ProjectNameRef(Guid.NewGuid(), "Axon Evidence Local"),
        };
        var result = Parser.Parse("revisar Axon Evidence Local", projects);

        result.ProjectId.Should().Be(projects[1].Id);
    }
}
