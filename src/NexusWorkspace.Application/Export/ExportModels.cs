namespace NexusWorkspace.Application.Export;

public enum ExportFormat
{
    Csv = 0,
    Json = 1,
    Excel = 2,
    Pdf = 3,
}

public sealed record ReportTaskRow(string Title, string Status, string Priority, string? Due, string? Assignee);

/// <summary>Everything a one-file project report contains, format-agnostic.</summary>
public sealed record ProjectReportData
{
    public required string ProjectName { get; init; }

    public string? Description { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? DueDateUtc { get; init; }

    public int ProgressPercent { get; init; }

    public DateTime GeneratedAtUtc { get; init; }

    public IReadOnlyList<ReportTaskRow> Tasks { get; init; } = [];

    public IReadOnlyList<string> FollowUps { get; init; } = [];

    public IReadOnlyList<string> Communications { get; init; } = [];

    public IReadOnlyList<string> Timeline { get; init; } = [];
}

/// <summary>A generic tabular export payload.</summary>
public sealed record TableExport
{
    public required string BaseName { get; init; }

    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; }
}
