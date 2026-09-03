using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NexusWorkspace.UI.ViewModels.CommandPalette;

namespace NexusWorkspace.UI.Views.CommandPalette;

public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
            Dispatcher.UIThread.Post(() => this.FindControl<TextBox>("Input")?.Focus(), DispatcherPriority.Input);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.RunCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                Move(viewModel, 1);
                e.Handled = true;
                break;

            case Key.Up:
                Move(viewModel, -1);
                e.Handled = true;
                break;
        }
    }

    private void OnResultActivated(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel viewModel)
        {
            viewModel.RunCommand.Execute(null);
        }
    }

    private static void Move(CommandPaletteViewModel viewModel, int delta)
    {
        if (viewModel.Results.Count == 0)
        {
            return;
        }

        var index = viewModel.Selected is null ? -1 : viewModel.Results.IndexOf(viewModel.Selected);
        index = (index + delta + viewModel.Results.Count) % viewModel.Results.Count;
        viewModel.Selected = viewModel.Results[index];
    }
}
