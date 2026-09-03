using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.UI.ViewModels;

namespace NexusWorkspace.UI.Views;

public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();

    private void OnOverlayBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only a click on the dark backdrop itself closes; clicks inside the card don't.
        if (ReferenceEquals(e.Source, sender) && DataContext is MainViewModel viewModel)
        {
            viewModel.CloseOverlaysCommand.Execute(null);
        }
    }
}
