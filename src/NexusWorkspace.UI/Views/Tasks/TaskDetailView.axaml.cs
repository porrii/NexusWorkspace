using Avalonia.Controls;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.Views.Tasks;

public partial class TaskDetailView : UserControl
{
    public TaskDetailView() => InitializeComponent();

    private void OnStatusSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1
            && e.AddedItems[0] is WorkTaskStatus status
            && DataContext is TaskDetailViewModel viewModel
            && viewModel.Detail is { } detail
            && detail.Status != status
            && viewModel.ChangeStatusCommand.CanExecute(status))
        {
            viewModel.ChangeStatusCommand.Execute(status);
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
