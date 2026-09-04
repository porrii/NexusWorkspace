using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Communications;

public sealed record LogCommunicationRequest
{
    public CommunicationChannel Channel { get; init; } = CommunicationChannel.Email;

    public CommunicationDirection Direction { get; init; } = CommunicationDirection.Outbound;

    public string Subject { get; init; } = string.Empty;

    public string? Body { get; init; }

    /// <summary>Defaults to "now" when null.</summary>
    public DateTime? OccurredAtUtc { get; init; }

    public Guid? PersonId { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? WorkTaskId { get; init; }

    public string? ContactLabel { get; init; }
}

public sealed record UpdateCommunicationRequest
{
    public required Guid Id { get; init; }

    public CommunicationChannel? Channel { get; init; }

    public CommunicationDirection? Direction { get; init; }

    public string? Subject { get; init; }

    public string? Body { get; init; }

    public DateTime? OccurredAtUtc { get; init; }
}
