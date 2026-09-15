using System;
using Avalonia;
using Avalonia.Controls;

namespace NexusWorkspace.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // WindowStartupLocation=CenterScreen can center a window that is taller/wider
        // than the working area, pushing its top (and the title bar) off-screen on
        // smaller or scaled displays. Shrink to fit and clamp back into view.
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var working = screen.WorkingArea;
        var scaling = screen.Scaling;
        var maxWidth = working.Width / scaling;
        var maxHeight = working.Height / scaling;

        if (Width > maxWidth)
        {
            Width = Math.Max(MinWidth, maxWidth);
        }

        if (Height > maxHeight)
        {
            Height = Math.Max(MinHeight, maxHeight);
        }

        var x = Math.Max(working.X, Math.Min(Position.X, working.Right - (int)(Width * scaling)));
        var y = Math.Max(working.Y, Math.Min(Position.Y, working.Bottom - (int)(Height * scaling)));
        Position = new PixelPoint(x, y);
    }
}
