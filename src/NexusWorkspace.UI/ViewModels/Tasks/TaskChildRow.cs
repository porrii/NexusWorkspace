using CommunityToolkit.Mvvm.ComponentModel;
using NexusWorkspace.Application.Tasks;

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

    /// <summary>Set only when shown inline on the global Tasks page, so the toggle command knows which row to refresh.</summary>
    public TaskListRow? OwnerRow { get; init; }

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editText = string.Empty;

    /// <summary>Subtasks first as a tree (parent then children, depth-first), then legacy checklist items as a flat tail.</summary>
    public static IEnumerable<TaskChildRow> BuildFrom(WorkTaskDetail detail, TaskListRow? owner = null)
    {
        var byParent = detail.SubTasks.ToLookup(s => s.ParentSubTaskId);

        IEnumerable<TaskChildRow> Walk(Guid? parent, int depth)
        {
            foreach (var s in byParent[parent].OrderBy(s => s.SortKey))
            {
                yield return new TaskChildRow { Id = s.Id, IsChecklist = false, Title = s.Title, IsDone = s.IsDone, Depth = depth, OwnerRow = owner };
                foreach (var child in Walk(s.Id, depth + 1))
                {
                    yield return child;
                }
            }
        }

        foreach (var row in Walk(null, 0))
        {
            yield return row;
        }

        foreach (var c in detail.Checklist.OrderBy(c => c.SortKey))
        {
            yield return new TaskChildRow { Id = c.Id, IsChecklist = true, Title = c.Text, IsDone = c.IsChecked, Depth = 0, OwnerRow = owner };
        }
    }
}
