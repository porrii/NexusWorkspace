using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
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
