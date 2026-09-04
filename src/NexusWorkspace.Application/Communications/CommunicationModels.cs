using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Communications;

/// <summary>One entry in a communication log (person, company, project or task feed).</summary>
public sealed record CommunicationListItem
{
    public required Guid Id { get; init; }

    public CommunicationChannel Channel { get; init; }

    public CommunicationDirection Direction { get; init; }

    public required string Subject { get; init; }

    public string? Body { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public Guid? PersonId { get; init; }

    public string? PersonName { get; init; }

    public Guid? CompanyId { get; init; }

    public string? CompanyName { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public Guid? WorkTaskId { get; init; }

    public string? WorkTaskTitle { get; init; }

    public string? ContactLabel { get; init; }

    /// <summary>Best available counterpart label: person, company or free text.</summary>
    public string With => PersonName ?? CompanyName ?? ContactLabel ?? "—";
}
