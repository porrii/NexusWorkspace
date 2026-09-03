using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NexusWorkspace.UI.ViewModels.QuickCapture;

namespace NexusWorkspace.UI.Views;

public partial class QuickCaptureWindow : Window
{
    public QuickCaptureWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();
    }

    public async Task PrepareAndShowAsync()
    {
        if (DataContext is QuickCaptureViewModel viewModel)
        {
            viewModel.RequestClose -= OnRequestClose;
            viewModel.RequestClose += OnRequestClose;
            await viewModel.PrepareAsync();
        }

        Show();
        Activate();

        Dispatcher.UIThread.Post(() =>
        {
            var input = this.FindControl<TextBox>("InputBox");
            input?.Focus();
        }, DispatcherPriority.Input);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnRequestClose(object? sender, EventArgs e) => Hide();
}
