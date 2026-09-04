using NexusWorkspace.Application.Calendar;

namespace NexusWorkspace.UI.ViewModels.Calendar;

/// <summary>One day cell in the month grid.</summary>
public sealed class CalendarDay
{
    public required DateOnly Date { get; init; }

    public int DayNumber => Date.Day;

    public bool InCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public IReadOnlyList<CalendarEntry> Entries { get; init; } = [];

    public IReadOnlyList<CalendarEntry> TopEntries => Entries.Count > 3 ? Entries.Take(3).ToList() : Entries;

    public int OverflowCount => Math.Max(0, Entries.Count - 3);

    public bool HasEntries => Entries.Count > 0;

    public string DayHeader => Date.ToString("dddd d 'de' MMMM");
}

public sealed class CalendarWeek
{
    public required IReadOnlyList<CalendarDay> Days { get; init; }
}
