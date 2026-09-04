using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Relations;

/// <summary>A resolved cross-reference as seen from one endpoint.</summary>
public sealed record RelationView
{
    public required Guid Id { get; init; }

    public RelationKind Kind { get; init; }

    /// <summary>True when the anchor entity is the <c>From</c> side of the stored pair.</summary>
    public bool IsOutgoing { get; init; }

    public EntityKind OtherKind { get; init; }

    public Guid OtherId { get; init; }

    public string OtherLabel { get; init; } = "—";

    /// <summary>The project to open for kinds that live inside a project (task, follow-up…).</summary>
    public Guid? OtherProjectId { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed record AddRelationRequest
{
    public required EntityKind FromKind { get; init; }

    public required Guid FromId { get; init; }

    public required EntityKind ToKind { get; init; }

    public required Guid ToId { get; init; }

    public RelationKind Kind { get; init; } = RelationKind.RelatesTo;

    public string? Note { get; init; }
}
