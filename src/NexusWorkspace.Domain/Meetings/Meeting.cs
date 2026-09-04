using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Domain.Meetings;

/// <summary>
/// A scheduled or held meeting. Carries its agenda, minutes and participant list,
/// optionally attached to a project. Appears in the calendar and in the
/// aggregated history of every participant.
/// </summary>
public class Meeting : AuditableEntity
{
    public required string Title { get; set; }

    public string? Agenda { get; set; }

    /// <summary>Minutes / decisions taken.</summary>
    public string? Notes { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    public string? Location { get; set; }

    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public ICollection<MeetingParticipant> Participants { get; } = new List<MeetingParticipant>();

    public bool IsUpcoming => Status == MeetingStatus.Scheduled && StartUtc >= DateTime.UtcNow;
}

/// <summary>A participant in a <see cref="Meeting"/>: a stored person or a free-text external name.</summary>
public class MeetingParticipant : Entity
{
    public Guid MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }

    /// <summary>Used when the participant is not (yet) a stored person.</summary>
    public string? ExternalName { get; set; }

    public string? Role { get; set; }

    public bool Attended { get; set; }
}
