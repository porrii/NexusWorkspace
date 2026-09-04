using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using NexusWorkspace.UI.ViewModels.Shared;

namespace NexusWorkspace.UI.Views.Shared;

public partial class AttachmentsPanel : UserControl
{
    public AttachmentsPanel()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(this, true);
    }

    private AttachmentsSectionViewModel? ViewModel => DataContext as AttachmentsSectionViewModel;

    private async void OnPickClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top is null)
        {
            return;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Adjuntar archivos",
            AllowMultiple = true,
        });

        await ViewModel.AddStorageFilesAsync(files);
    }

    private async void OnPasteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is null)
        {
            return;
        }

        try
        {
            var formats = await top.Clipboard.GetFormatsAsync();
            if (!formats.Contains(DataFormats.Files))
            {
                return;
            }

            var data = await top.Clipboard.GetDataAsync(DataFormats.Files);
            if (data is IEnumerable<IStorageItem> items)
            {
                await ViewModel.AddStorageFilesAsync(items.OfType<IStorageFile>().ToList());
            }
        }
        catch (Exception)
        {
            // Clipboard access can fail transiently; ignore.
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        if (ViewModel is not null)
        {
            ViewModel.IsDragActive = e.Data.Contains(DataFormats.Files);
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsDragActive = false;
        }

        var files = e.Data.GetFiles()?.OfType<IStorageFile>().ToList();
        if (files is { Count: > 0 } && ViewModel is not null)
        {
            await ViewModel.AddStorageFilesAsync(files);
        }
    }
}
