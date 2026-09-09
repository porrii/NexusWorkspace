using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.UI.ViewModels.Companies;

namespace NexusWorkspace.UI.Views.Companies;

public partial class CompanyDetailView : UserControl
{
    public CompanyDetailView() => InitializeComponent();

    private void OnAddTagSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is TagListItem tag
            && DataContext is CompanyDetailViewModel viewModel)
        {
            viewModel.AddTagCommand.Execute(tag);
            combo.SelectedItem = null;
        }
    }

    private void OnAddPersonSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is PersonListItem person
            && DataContext is CompanyDetailViewModel viewModel)
        {
            viewModel.AddPersonCommand.Execute(person);
            combo.SelectedItem = null;
        }
    }

    private void OnLinkProjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.SelectedItem is ProjectListItem project
            && DataContext is CompanyDetailViewModel viewModel)
        {
            viewModel.LinkProjectCommand.Execute(project);
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
            && DataContext is CompanyDetailViewModel viewModel
            && viewModel.OpenRelationCommand.CanExecute(relation))
        {
            viewModel.OpenRelationCommand.Execute(relation);
        }
    }
}
