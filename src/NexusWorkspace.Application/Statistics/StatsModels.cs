namespace NexusWorkspace.Application.Statistics;

public sealed record NamedCount(string Name, int Count);

public sealed record WeekPoint(DateOnly WeekStart, int Count);

/// <summary>A snapshot of the whole workspace for the Estadísticas screen.</summary>
public sealed record WorkspaceStats
{
    public int TotalProjectCount { get; init; }

    public int ActiveProjectCount { get; init; }

    public int TotalTaskCount { get; init; }

    public int OpenTaskCount { get; init; }

    public int FinishedTaskCount { get; init; }

    public int OverdueTaskCount { get; init; }

    public int OpenFollowUpCount { get; init; }

    public int PeopleCount { get; init; }

    public int CompanyCount { get; init; }

    public int AttachmentCount { get; init; }

    public long AttachmentBytes { get; init; }

    public int CommunicationsLast30 { get; init; }

    public int IncidentsLast30 { get; init; }

    public double AvgDaysToFinish { get; init; }

    public IReadOnlyList<NamedCount> ProjectsByStatus { get; init; } = [];

    public IReadOnlyList<NamedCount> TasksByStatus { get; init; } = [];

    public IReadOnlyList<NamedCount> TasksByPriority { get; init; } = [];

    public IReadOnlyList<WeekPoint> FinishedPerWeek { get; init; } = [];

    public IReadOnlyList<NamedCount> TopPeopleByOpenTasks { get; init; } = [];

    public IReadOnlyList<NamedCount> ActivityByTypeLast30 { get; init; } = [];
}
