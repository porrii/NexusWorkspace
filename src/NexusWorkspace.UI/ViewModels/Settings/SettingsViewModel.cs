using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Import;
using NexusWorkspace.Application.Settings;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _theme;
    private readonly ISettingsStore _settings;
    private readonly IAppPaths _paths;
    private readonly IPlatformLauncher _launcher;
    private readonly IUnitOfWorkRunner _unitOfWork;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private bool _demoDataPresent;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ImportSource _importSource = ImportSource.Notes;

    [ObservableProperty]
    private string _importText = string.Empty;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    [ObservableProperty]
    private ImportPreview? _importPreview;

    public SettingsViewModel(
        IThemeService theme,
        ISettingsStore settings,
        IAppPaths paths,
        IPlatformLauncher launcher,
        IUnitOfWorkRunner unitOfWork)
    {
        _theme = theme;
        _settings = settings;
        _paths = paths;
        _launcher = launcher;
        _unitOfWork = unitOfWork;
        _selectedTheme = settings.Current.Theme;
    }

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<ImportSource> ImportSources { get; } = Enum.GetValues<ImportSource>();

    public bool HasPreview => ImportPreview is { ProjectCount: > 0 };

    public string DataFolder => _paths.RootDirectory;

    public bool IsPortable => _paths.IsPortable;

    public override async Task OnActivatedAsync()
    {
        SelectedTheme = _settings.Current.Theme;
        DemoDataPresent = await _unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<IDemoDataService>().IsPresentAsync(ct));
    }

    partial void OnSelectedThemeChanged(ThemeMode value) => _ = _theme.SetAndPersistAsync(value);

    [RelayCommand]
    private void OpenDataFolder() => _launcher.OpenFolder(_paths.RootDirectory);

    [RelayCommand]
    private void OpenLogsFolder() => _launcher.OpenFolder(_paths.LogsDirectory);

    [RelayCommand]
    private async Task LoadDemoDataAsync()
    {
        IsBusy = true;
        try
        {
            await _unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<IDemoDataService>().SeedAsync(ct));
            DemoDataPresent = true;
            StatusMessage = "Datos de demostración cargados.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo cargar la demo: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnImportPreviewChanged(ImportPreview? value) => OnPropertyChanged(nameof(HasPreview));

    [RelayCommand]
    private async Task PreviewImportAsync()
    {
        ImportPreview = null;
        var result = await _unitOfWork.RunAsync((sp, ct) =>
            Task.FromResult(sp.GetRequiredService<ImportService>().Parse(ImportSource, ImportText)));

        if (result.IsFailure)
        {
            ImportStatus = result.Error.Message;
            return;
        }

        ImportPreview = result.Value;
        var warnings = ImportPreview.Warnings.Count > 0 ? $" · {ImportPreview.Warnings.Count} aviso(s)" : string.Empty;
        ImportStatus = $"Se crearán {ImportPreview.ProjectCount} proyecto(s) y {ImportPreview.TaskCount} tarea(s).{warnings}";
    }

    [RelayCommand]
    private async Task CommitImportAsync()
    {
        if (ImportPreview is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var preview = ImportPreview;
            var result = await _unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ImportService>().CommitAsync(preview, ct));

            if (result.IsFailure)
            {
                ImportStatus = result.Error.Message;
                return;
            }

            ImportStatus = $"Importados {result.Value} proyecto(s).";
            ImportPreview = null;
            ImportText = string.Empty;
        }
        catch (Exception ex)
        {
            ImportStatus = $"No se pudo importar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveDemoDataAsync()
    {
        IsBusy = true;
        try
        {
            await _unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<IDemoDataService>().RemoveAsync(ct));
            DemoDataPresent = false;
            StatusMessage = "Datos de demostración eliminados.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar la demo: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
