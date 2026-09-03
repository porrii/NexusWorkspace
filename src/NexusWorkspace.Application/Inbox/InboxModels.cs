using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.Inbox;

public sealed record InboxItemView
{
    public required Guid Id { get; init; }

    public required string RawText { get; init; }

    public string? ParsedHint { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed record ConvertToTaskOptions
{
    public string? Title { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? DueDateUtc { get; init; }
}

public sealed record ConvertToProjectOptions
{
    public string? Name { get; init; }

    public Priority Priority { get; init; } = Priority.Medium;
}
