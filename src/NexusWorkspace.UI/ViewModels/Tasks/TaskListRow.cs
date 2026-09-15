using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NexusWorkspace.Application.Tasks;

namespace NexusWorkspace.UI.ViewModels.Tasks;

/// <summary>One row on the global "Tareas" page: a task plus its subtasks/comments, loaded inline on first expand.</summary>
public sealed partial class TaskListRow : ObservableObject
{
    public required WorkTaskListItem Item { get; init; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isLoadingChildren;

    [ObservableProperty]
    private bool _childrenLoaded;

    /// <summary>Subtasks and legacy checklist items, merged; populated on first expand.</summary>
    public ObservableCollection<TaskChildRow> Children { get; } = [];

    /// <summary>Shown instead of <see cref="Children"/> when the task has no subtasks/checklist items.</summary>
    public ObservableCollection<TaskCommentView> Comments { get; } = [];
}
