using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.UI.ViewModels.Companies;

namespace NexusWorkspace.UI.Views.Companies;

public partial class CompaniesView : UserControl
{
    public CompaniesView() => InitializeComponent();

    private void OnCompanyClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source is Avalonia.Visual source && source.FindAncestorOfType<Button>() is not null)
        {
            return;
        }

        if (sender is Control { Tag: CompanyListItem item }
            && DataContext is CompaniesViewModel viewModel
            && viewModel.OpenCompanyCommand.CanExecute(item))
        {
            viewModel.OpenCompanyCommand.Execute(item);
        }
    }
}
