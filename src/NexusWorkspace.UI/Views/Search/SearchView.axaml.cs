using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NexusWorkspace.Application.Search;
using NexusWorkspace.UI.ViewModels.Search;

namespace NexusWorkspace.UI.Views.Search;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
            Dispatcher.UIThread.Post(() => this.FindControl<TextBox>("Input")?.Focus(), DispatcherPriority.Input);
    }

    private void OnActivate(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: SearchHit hit }
            && DataContext is SearchViewModel viewModel
            && viewModel.OpenCommand.CanExecute(hit))
        {
            viewModel.OpenCommand.Execute(hit);
        }
    }
}
