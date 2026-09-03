using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.Views.Projects;

public partial class ProjectDetailView : UserControl
{
    public ProjectDetailView() => InitializeComponent();

    private void OnTaskClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: WorkTaskListItem item }
            && DataContext is ProjectDetailViewModel viewModel
            && viewModel.OpenTaskCommand.CanExecute(item))
        {
            viewModel.OpenTaskCommand.Execute(item);
        }
    }

    private void OnStatusSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 1
            && e.AddedItems[0] is ProjectStatus status
            && DataContext is ProjectDetailViewModel viewModel
            && viewModel.Header is { } header
            && header.Status != status
            && viewModel.ChangeStatusCommand.CanExecute(status))
        {
            viewModel.ChangeStatusCommand.Execute(status);
        }
    }
}
