using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.UI.ViewModels.FollowUps;

namespace NexusWorkspace.UI.Views.FollowUps;

public partial class FollowUpsView : UserControl
{
    public FollowUpsView() => InitializeComponent();

    private void OnRowClicked(object? sender, PointerReleasedEventArgs e)
    {
        // Ignore clicks that landed on one of the action buttons inside the row.
        if (e.Source is Visual source && source.FindAncestorOfType<Button>() is not null)
        {
            return;
        }

        if (sender is Control { Tag: FollowUpListItem item }
            && DataContext is FollowUpsViewModel viewModel
            && viewModel.OpenCommand.CanExecute(item))
        {
            viewModel.OpenCommand.Execute(item);
        }
    }
}
