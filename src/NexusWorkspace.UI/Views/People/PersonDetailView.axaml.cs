using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.Projects;
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
            combo.SelectedItem = null;
            viewModel.AddTagCommand.Execute(tag);
        }
    }

    private void OnLinkProjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is ProjectListItem project
            && DataContext is PersonDetailViewModel viewModel)
        {
            combo.SelectedItem = null;
            viewModel.LinkProjectCommand.Execute(project);
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
