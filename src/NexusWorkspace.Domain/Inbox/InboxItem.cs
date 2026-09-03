using NexusWorkspace.Domain.Common;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Domain.Inbox;

/// <summary>
/// A fast, unclassified capture. Kept until the user converts it into a task,
/// project, incident or note — or dismisses it. Nothing is lost: dismissed items
/// are soft-deleted, converted items keep a link to what they became.
/// </summary>
public class InboxItem : AuditableEntity
{
    public required string RawText { get; set; }

    /// <summary>One-line, human-readable summary of what the local parser suggested.</summary>
    public string? ParsedHint { get; set; }

    public InboxItemState State { get; set; } = InboxItemState.Pending;

    /// <summary>What the item was turned into, once converted.</summary>
    public EntityKind? ConvertedToKind { get; set; }

    public Guid? ConvertedToId { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
}

public enum InboxItemState
{
    Pending = 0,
    Converted = 1,
    Dismissed = 2,
}
