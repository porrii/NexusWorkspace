using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.FollowUps;

public sealed record StartFollowUpRequest
{
    public required EntityKind TargetKind { get; init; }

    public required Guid TargetId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public Guid? WaitingOnPersonId { get; init; }

    public Guid? WaitingOnCompanyId { get; init; }

    /// <summary>Free-text party, e.g. "Sistemas", "Cliente", when not a stored person/company.</summary>
    public string? WaitingOnLabel { get; init; }

    public DateTime? NextFollowUpUtc { get; init; }
}

public sealed record ResolveFollowUpRequest
{
    public required Guid Id { get; init; }

    /// <summary><see cref="FollowUpState.Answered"/> or <see cref="FollowUpState.Closed"/>.</summary>
    public FollowUpState State { get; init; } = FollowUpState.Answered;

    public string? Resolution { get; init; }
}
