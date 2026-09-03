using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.UI.ViewModels.Dashboard;

namespace NexusWorkspace.UI.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView() => InitializeComponent();

    private void OnProjectRowClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: ProjectListItem item }
            && DataContext is DashboardViewModel viewModel
            && viewModel.OpenProjectCommand.CanExecute(item))
        {
            viewModel.OpenProjectCommand.Execute(item);
        }
    }
}
