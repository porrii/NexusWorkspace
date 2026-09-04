using System.Collections.ObjectModel;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.ViewModels.Projects;

/// <summary>One column of the project Kanban board.</summary>
public sealed class KanbanColumn(WorkTaskStatus status, string title)
{
    public WorkTaskStatus Status { get; } = status;

    public string Title { get; } = title;

    public ObservableCollection<WorkTaskListItem> Cards { get; } = [];

    public int Count => Cards.Count;
}

/// <summary>Activity entries for one calendar day, for the project "Cronología" tab.</summary>
public sealed class TimelineDay(DateOnly date, string label)
{
    public DateOnly Date { get; } = date;

    public string Label { get; } = label;

    public ObservableCollection<ActivityEntry> Events { get; } = [];
}
