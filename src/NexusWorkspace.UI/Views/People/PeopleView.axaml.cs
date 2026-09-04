using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.People;
using NexusWorkspace.UI.ViewModels.People;

namespace NexusWorkspace.UI.Views.People;

public partial class PeopleView : UserControl
{
    public PeopleView() => InitializeComponent();

    private void OnPersonClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source is Avalonia.Visual source && source.FindAncestorOfType<Button>() is not null)
        {
            return;
        }

        if (sender is Control { Tag: PersonListItem item }
            && DataContext is PeopleViewModel viewModel
            && viewModel.OpenPersonCommand.CanExecute(item))
        {
            viewModel.OpenPersonCommand.Execute(item);
        }
    }
}
