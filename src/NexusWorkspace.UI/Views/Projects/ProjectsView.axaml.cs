using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.Views.Projects;

public partial class ProjectsView : UserControl
{
    public ProjectsView() => InitializeComponent();

    private void OnProjectClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: ProjectListItem item }
            && DataContext is ProjectsViewModel viewModel
            && viewModel.OpenProjectCommand.CanExecute(item))
        {
            viewModel.OpenProjectCommand.Execute(item);
        }
    }
}
