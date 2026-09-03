using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Shell;

namespace NexusWorkspace.UI.ViewModels;

/// <summary>The shell: navigation rail, active page host, theme and sidebar state.</summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IThemeService _theme;
    private readonly ISettingsStore _settings;
    private bool _suppressSelectionNavigation;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private string _themeGlyph = "◐";

    public MainViewModel(INavigationService navigation, IThemeService theme, ISettingsStore settings)
    {
        _navigation = navigation;
        _theme = theme;
        _settings = settings;

        IsSidebarExpanded = settings.Current.SidebarExpanded;
        UpdateThemeGlyph(settings.Current.Theme);

        Items =
        [
            new(PageKey.Dashboard, "Dashboard", "ViewDashboard"),
            new(PageKey.Inbox, "Inbox", "Inbox", IsImplemented: false),
            new(PageKey.Projects, "Proyectos", "FolderMultiple"),
            new(PageKey.Tasks, "Tareas", "CheckboxMarked", IsImplemented: false),
            new(PageKey.FollowUps, "Seguimientos", "ClockAlert", IsImplemented: false),
            new(PageKey.Calendar, "Calendario", "Calendar", IsImplemented: false),
            new(PageKey.People, "Personas", "AccountMultiple", IsImplemented: false),
            new(PageKey.Companies, "Empresas", "OfficeBuilding", IsImplemented: false),
            new(PageKey.Files, "Archivos", "Paperclip", IsImplemented: false),
            new(PageKey.Activity, "Actividad", "History"),
            new(PageKey.Statistics, "Estadísticas", "ChartBox", IsImplemented: false),
            new(PageKey.Archived, "Archivados", "Archive", IsImplemented: false),
            new(PageKey.Settings, "Configuración", "Cog"),
        ];

        _navigation.Navigated += OnNavigated;
        _navigation.NavigateTo(PageKey.Dashboard);
    }

    public ObservableCollection<NavigationItem> Items { get; }

    public bool CanGoBack => _navigation.CanGoBack;

    partial void OnSelectedItemChanged(NavigationItem? value)
    {
        if (_suppressSelectionNavigation || value is null)
        {
            return;
        }

        _navigation.NavigateTo(value.Key);
    }

    private void OnNavigated(object? sender, ViewModelBase page)
    {
        CurrentPage = page;
        OnPropertyChanged(nameof(CanGoBack));

        var key = PageKeyFor(page);
        var match = key is null ? null : Items.FirstOrDefault(i => i.Key == key);
        if (match is not null && !ReferenceEquals(match, SelectedItem))
        {
            _suppressSelectionNavigation = true;
            SelectedItem = match;
            _suppressSelectionNavigation = false;
        }
    }

    private static PageKey? PageKeyFor(ViewModelBase page) => page switch
    {
        Dashboard.DashboardViewModel => PageKey.Dashboard,
        Projects.ProjectsViewModel => PageKey.Projects,
        Projects.ProjectDetailViewModel => PageKey.Projects,
        Tasks.TaskDetailViewModel => PageKey.Projects,
        Activity.ActivityViewModel => PageKey.Activity,
        Settings.SettingsViewModel => PageKey.Settings,
        PlaceholderViewModel placeholder => placeholder.Key,
        _ => null,
    };

    [RelayCommand]
    private async Task ToggleSidebarAsync()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        await _settings.UpdateAsync(s => s.SidebarExpanded = IsSidebarExpanded);
    }

    [RelayCommand]
    private async Task CycleThemeAsync()
    {
        var next = _settings.Current.Theme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

        await _theme.SetAndPersistAsync(next);
        UpdateThemeGlyph(next);
    }

    [RelayCommand]
    private void GoBack() => _navigation.GoBack();

    private void UpdateThemeGlyph(ThemeMode mode) => ThemeGlyph = mode switch
    {
        ThemeMode.Light => "☀",
        ThemeMode.Dark => "☾",
        _ => "◐",
    };
}
