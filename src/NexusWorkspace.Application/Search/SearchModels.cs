using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Search;

/// <summary>One result from the global search.</summary>
public sealed record SearchHit
{
    /// <summary>Kind of the row that matched (may be a comment).</summary>
    public EntityKind EntityKind { get; init; }

    public Guid EntityId { get; init; }

    /// <summary>Kind of the thing the user should open (the comment's parent task, say).</summary>
    public EntityKind NavigateKind { get; init; }

    public Guid NavigateId { get; init; }

    public Guid? ProjectId { get; init; }

    public required string Title { get; init; }

    /// <summary>A short snippet of the matching body text, with the match marked.</summary>
    public string? Snippet { get; init; }

    /// <summary>Lower is more relevant (FTS5 bm25 rank).</summary>
    public double Rank { get; init; }
}
