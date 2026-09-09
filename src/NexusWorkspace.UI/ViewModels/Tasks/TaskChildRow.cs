using CommunityToolkit.Mvvm.ComponentModel;

namespace NexusWorkspace.UI.ViewModels.Tasks;

/// <summary>
/// One row in the unified "Subtareas" list of a task: a real <c>SubTask</c> or a
/// legacy <c>ChecklistItem</c>. The two entities are being merged; until the data
/// migration lands both kinds are shown together and edited in place.
/// </summary>
public sealed partial class TaskChildRow : ObservableObject
{
    public required Guid Id { get; init; }

    /// <summary>True = legacy checklist item, false = subtask.</summary>
    public required bool IsChecklist { get; init; }

    public required string Title { get; init; }

    public required bool IsDone { get; init; }

    /// <summary>Nesting level for subtasks (0 for checklist items).</summary>
    public int Depth { get; init; }

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editText = string.Empty;
}
