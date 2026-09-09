using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.Application.People;
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

    private void OnAddCollaboratorSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is PersonListItem person
            && DataContext is TaskDetailViewModel viewModel)
        {
            viewModel.AddCollaboratorCommand.Execute(person);
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

    private void OnChildTitleDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: TaskChildRow row }
            && DataContext is TaskDetailViewModel viewModel)
        {
            viewModel.StartRenameChildCommand.Execute(row);
        }
    }

    private void OnChildEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Control { DataContext: TaskChildRow row }
            || DataContext is not TaskDetailViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            viewModel.CommitRenameChildCommand.Execute(row);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            viewModel.CancelRenameChildCommand.Execute(row);
            e.Handled = true;
        }
    }

    private void OnNewSubTaskKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TaskDetailViewModel viewModel)
        {
            viewModel.AddSubTaskCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnNewEventKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TaskDetailViewModel viewModel)
        {
            viewModel.LogEventCommand.Execute(null);
            e.Handled = true;
        }
    }
}
