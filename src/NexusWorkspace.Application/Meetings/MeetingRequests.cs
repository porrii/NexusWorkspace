namespace NexusWorkspace.Application.Meetings;

/// <summary>A participant line when scheduling a meeting: a stored person or a free-text name.</summary>
public sealed record MeetingParticipantInput
{
    public Guid? PersonId { get; init; }

    public string? ExternalName { get; init; }

    public string? Role { get; init; }
}

public sealed record ScheduleMeetingRequest
{
    public string Title { get; init; } = string.Empty;

    public string? Agenda { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }

    public string? Location { get; init; }

    public Guid? ProjectId { get; init; }

    public IReadOnlyList<MeetingParticipantInput> Participants { get; init; } = [];
}

public sealed record UpdateMeetingRequest
{
    public required Guid Id { get; init; }

    public string? Title { get; init; }

    public string? Agenda { get; init; }

    public string? Notes { get; init; }

    public DateTime? StartUtc { get; init; }

    public DateTime? EndUtc { get; init; }

    public string? Location { get; init; }
}
