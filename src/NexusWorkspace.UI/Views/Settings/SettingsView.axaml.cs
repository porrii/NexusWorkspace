using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NexusWorkspace.UI.ViewModels.Settings;

namespace NexusWorkspace.UI.Views.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    private async void OnExportWorkspaceClicked(object? sender, RoutedEventArgs e)
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

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar workspace",
            SuggestedFileName = $"nexus-workspace-{System.DateTime.Now:yyyyMMdd}.zip",
            DefaultExtension = "zip",
            FileTypeChoices = [new FilePickerFileType("Workspace de NexusWorkspace") { Patterns = ["*.zip"] }],
        });

        if (file?.TryGetLocalPath() is { } path)
        {
            await ViewModel.ExportWorkspaceAsync(path);
        }
    }

    private async void OnImportWorkspaceClicked(object? sender, RoutedEventArgs e)
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
            Title = "Importar workspace",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Workspace de NexusWorkspace") { Patterns = ["*.zip"] }],
        });

        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            await ViewModel.ImportWorkspaceAsync(path);
        }
    }
}
