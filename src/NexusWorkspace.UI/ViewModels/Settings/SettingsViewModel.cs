using System.Collections.ObjectModel;
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
    private readonly IBackupService _backup;
    private bool _loadingSettings;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private bool _demoDataPresent;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _backupSchedule = "Daily";

    [ObservableProperty]
    private int _keepVersions = 30;

    [ObservableProperty]
    private bool _backupBeforeMigration = true;

    [ObservableProperty]
    private bool _includeFilesInBackup;

    [ObservableProperty]
    private string _backupStatus = string.Empty;

    [ObservableProperty]
    private bool _restartRequired;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _remindersOnStartup = true;

    [ObservableProperty]
    private bool _followUpNudges = true;

    [ObservableProperty]
    private string _startupSection = "Dashboard";

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
        IUnitOfWorkRunner unitOfWork,
        IBackupService backup)
    {
        _theme = theme;
        _settings = settings;
        _paths = paths;
        _launcher = launcher;
        _unitOfWork = unitOfWork;
        _backup = backup;
        _selectedTheme = settings.Current.Theme;
        LoadFromSettings();
    }

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<ImportSource> ImportSources { get; } = Enum.GetValues<ImportSource>();

    public IReadOnlyList<string> BackupScheduleOptions { get; } = ["None", "Daily", "Weekly"];

    public IReadOnlyList<string> StartupSectionOptions { get; } =
        ["Dashboard", "Inbox", "Projects", "FollowUps", "Calendar", "Statistics"];

    public ObservableCollection<BackupInfo> Backups { get; } = [];

    public bool HasPreview => ImportPreview is { ProjectCount: > 0 };

    public string DataFolder => _paths.RootDirectory;

    public bool IsPortable => _paths.IsPortable;

    public override async Task OnActivatedAsync()
    {
        SelectedTheme = _settings.Current.Theme;
        LoadFromSettings();
        DemoDataPresent = await _unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<IDemoDataService>().IsPresentAsync(ct));
        await RefreshBackupsAsync();
    }

    private void LoadFromSettings()
    {
        _loadingSettings = true;
        var s = _settings.Current;
        BackupSchedule = s.Backup.Schedule;
        KeepVersions = s.Backup.KeepVersions;
        BackupBeforeMigration = s.Backup.BackupBeforeMigration;
        NotificationsEnabled = s.Notifications.Enabled;
        RemindersOnStartup = s.Notifications.RemindersOnStartup;
        FollowUpNudges = s.Notifications.FollowUpNudges;
        StartupSection = s.StartupSection;
        _loadingSettings = false;
    }

    private async Task RefreshBackupsAsync()
    {
        try
        {
            var list = await _backup.ListAsync();
            Backups.Clear();
            foreach (var b in list)
            {
                Backups.Add(b);
            }
        }
        catch (Exception ex)
        {
            BackupStatus = $"No se pudo leer la lista de copias: {ex.Message}";
        }
    }

    partial void OnSelectedThemeChanged(ThemeMode value) => _ = _theme.SetAndPersistAsync(value);

    partial void OnBackupScheduleChanged(string value) => Persist(s => s.Backup.Schedule = value);

    partial void OnKeepVersionsChanged(int value) => Persist(s => s.Backup.KeepVersions = Math.Clamp(value, 1, 999));

    partial void OnBackupBeforeMigrationChanged(bool value) => Persist(s => s.Backup.BackupBeforeMigration = value);

    partial void OnNotificationsEnabledChanged(bool value) => Persist(s => s.Notifications.Enabled = value);

    partial void OnRemindersOnStartupChanged(bool value) => Persist(s => s.Notifications.RemindersOnStartup = value);

    partial void OnFollowUpNudgesChanged(bool value) => Persist(s => s.Notifications.FollowUpNudges = value);

    partial void OnStartupSectionChanged(string value) => Persist(s => s.StartupSection = value);

    private void Persist(Action<AppSettings> mutate)
    {
        if (!_loadingSettings)
        {
            _ = _settings.UpdateAsync(mutate);
        }
    }

    [RelayCommand]
    private async Task CreateBackupNowAsync()
    {
        IsBusy = true;
        try
        {
            var info = await _backup.CreateAsync("manual", IncludeFilesInBackup);
            BackupStatus = $"Copia creada: {info.FileName} ({info.SizeBytes / 1024} KB).";
            await RefreshBackupsAsync();
        }
        catch (Exception ex)
        {
            BackupStatus = $"No se pudo crear la copia: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(BackupInfo? info)
    {
        if (info is null)
        {
            return;
        }

        try
        {
            await _backup.StageRestoreAsync(info.Id);
            RestartRequired = true;
            BackupStatus = "Restauración preparada. Cierra y vuelve a abrir la aplicación para aplicarla.";
        }
        catch (Exception ex)
        {
            BackupStatus = $"No se pudo preparar la restauración: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteBackupAsync(BackupInfo? info)
    {
        if (info is null)
        {
            return;
        }

        await _backup.DeleteAsync(info.Id);
        await RefreshBackupsAsync();
    }

    [RelayCommand]
    private void OpenBackupsFolder() => _launcher.OpenFolder(_paths.BackupsDirectory);

    public async Task ExportWorkspaceAsync(string destinationPath)
    {
        try
        {
            var path = await _backup.ExportToAsync(destinationPath, includeFiles: true);
            BackupStatus = $"Workspace exportado a {path}.";
            _launcher.RevealInFolder(path);
        }
        catch (Exception ex)
        {
            BackupStatus = $"No se pudo exportar: {ex.Message}";
        }
    }

    public async Task ImportWorkspaceAsync(string zipPath)
    {
        try
        {
            await _backup.StageImportAsync(zipPath);
            RestartRequired = true;
            BackupStatus = "Importación preparada. Cierra y vuelve a abrir la aplicación para aplicarla.";
        }
        catch (Exception ex)
        {
            BackupStatus = $"No se pudo importar: {ex.Message}";
        }
    }

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
