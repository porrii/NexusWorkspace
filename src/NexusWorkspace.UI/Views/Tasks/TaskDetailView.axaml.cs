using Avalonia.Controls;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.Views.Tasks;

public partial class TaskDetailView : UserControl
{
    public TaskDetailView() => InitializeComponent();

    private void OnAddTagSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is TagListItem tag
            && DataContext is TaskDetailViewModel viewModel)
        {
            viewModel.AddTagCommand.Execute(tag);
            combo.SelectedItem = null;
        }
    }

    private void OnPrioritySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1
            && e.AddedItems[0] is Priority priority
            && DataContext is TaskDetailViewModel viewModel
            && viewModel.Detail is { } detail
            && detail.Priority != priority
            && viewModel.ChangePriorityCommand.CanExecute(priority))
        {
            viewModel.ChangePriorityCommand.Execute(priority);
        }
    }
}
