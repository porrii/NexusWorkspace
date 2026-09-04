using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Meetings;

public sealed record MeetingParticipantView
{
    public required Guid Id { get; init; }

    public Guid? PersonId { get; init; }

    public required string Name { get; init; }

    public string? Role { get; init; }

    public bool Attended { get; init; }
}

/// <summary>Row in a meeting list (project, person feed, calendar).</summary>
public sealed record MeetingListItem
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Agenda { get; init; }

    public string? Notes { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }

    public string? Location { get; init; }

    public MeetingStatus Status { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public int ParticipantCount { get; init; }

    public IReadOnlyList<MeetingParticipantView> Participants { get; init; } = [];

    public bool IsUpcoming => Status == MeetingStatus.Scheduled && StartUtc >= DateTime.UtcNow;
}

public enum MeetingScope
{
    Upcoming = 0,
    Past = 1,
    All = 2,
}
