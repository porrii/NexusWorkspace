using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;

namespace NexusWorkspace.Domain.FollowUps;

/// <summary>
/// "Waiting for a reply from…". Attached to a project or a task, points at the
/// person / company / area we are chasing, and tracks how long we have waited,
/// the last contact, the next planned nudge and how many reminders we have sent.
/// </summary>
public class FollowUp : AuditableEntity
{
    public EntityKind TargetKind { get; set; }

    public Guid TargetId { get; set; }

    /// <summary>Denormalised for the dashboard and per-project queries.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>What we are waiting for, e.g. "Confirmación del endpoint de PRE".</summary>
    public required string Subject { get; set; }

    public Guid? WaitingOnPersonId { get; set; }

    public Person? WaitingOnPerson { get; set; }

    public Guid? WaitingOnCompanyId { get; set; }

    public Company? WaitingOnCompany { get; set; }

    /// <summary>Free-text party when it is not a stored person/company, e.g. "Sistemas", "Cliente".</summary>
    public string? WaitingOnLabel { get; set; }

    public DateTime WaitingSinceUtc { get; set; }

    public DateTime? LastContactUtc { get; set; }

    public DateTime? NextFollowUpUtc { get; set; }

    public int ReminderCount { get; set; }

    public FollowUpState State { get; set; } = FollowUpState.Waiting;

    public DateTime? ResolvedAtUtc { get; set; }

    public string? Resolution { get; set; }

    public bool IsOpen => State is FollowUpState.Waiting or FollowUpState.Escalated;

    public int DaysWaiting => Math.Max(0, (int)Math.Floor((DateTime.UtcNow - WaitingSinceUtc).TotalDays));

    public int DaysSinceLastContact =>
        Math.Max(0, (int)Math.Floor((DateTime.UtcNow - (LastContactUtc ?? WaitingSinceUtc)).TotalDays));
}
