using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.Application.QuickCapture;

/// <summary>A project the parser can match by name (id + name only).</summary>
public sealed record ProjectNameRef(Guid Id, string Name);

/// <summary>
/// What the local rule-based parser understood from a quick-capture line.
/// Everything is a <em>suggestion</em>: it pre-fills fields, it never applies
/// changes on its own.
/// </summary>
public sealed record QuickCaptureParseResult
{
    /// <summary>The original text, untouched.</summary>
    public required string RawText { get; init; }

    /// <summary>Text with <c>#tag</c>, <c>@person</c> and <c>!priority</c> tokens removed.</summary>
    public required string CleanText { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public Priority? Priority { get; init; }

    public DateTime? DueDateUtc { get; init; }

    /// <summary>A leading action verb, e.g. "Enviar", "Revisar", "Llamar".</summary>
    public string? Action { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<string> People { get; init; } = [];

    public bool HasAnySuggestion =>
        ProjectId is not null || Priority is not null || DueDateUtc is not null
        || Action is not null || Tags.Count > 0 || People.Count > 0;
}
