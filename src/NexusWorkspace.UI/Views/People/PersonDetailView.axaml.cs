using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.UI.ViewModels.People;

namespace NexusWorkspace.UI.Views.People;

public partial class PersonDetailView : UserControl
{
    public PersonDetailView() => InitializeComponent();

    private void OnAddTagSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is TagListItem tag
            && DataContext is PersonDetailViewModel viewModel)
        {
            viewModel.AddTagCommand.Execute(tag);
            combo.SelectedItem = null;
        }
    }

    private void OnRelationClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source is Avalonia.Visual source && source.FindAncestorOfType<Button>() is not null)
        {
            return;
        }

        if (sender is Control { Tag: RelationView relation }
            && DataContext is PersonDetailViewModel viewModel
            && viewModel.OpenRelationCommand.CanExecute(relation))
        {
            viewModel.OpenRelationCommand.Execute(relation);
        }
    }
}
