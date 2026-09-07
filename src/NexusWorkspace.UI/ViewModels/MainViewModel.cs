using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.CommandPalette;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Search;
using NexusWorkspace.UI.ViewModels.Shell;

namespace NexusWorkspace.UI.ViewModels;

/// <summary>The shell: navigation rail, active page host, theme, sidebar, and the Ctrl+K / Ctrl+F overlays.</summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IThemeService _theme;
    private readonly ISettingsStore _settings;
    private readonly IQuickCaptureLauncher _quickCapture;
    private bool _suppressSelectionNavigation;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private string _themeGlyph = "◐";

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private bool _isSearchOpen;

    public MainViewModel(
        INavigationService navigation,
        IThemeService theme,
        ISettingsStore settings,
        IQuickCaptureLauncher quickCapture,
        CommandPaletteViewModel palette,
        SearchViewModel search,
        NotificationCenterViewModel notifications)
    {
        _navigation = navigation;
        _theme = theme;
        _settings = settings;
        _quickCapture = quickCapture;
        Palette = palette;
        Search = search;
        Notifications = notifications;

        IsSidebarExpanded = settings.Current.SidebarExpanded;
        UpdateThemeGlyph(settings.Current.Theme);

        Items =
        [
            new(PageKey.Dashboard, "Dashboard", "ViewDashboard"),
            new(PageKey.Inbox, "Inbox", "Inbox"),
            new(PageKey.Projects, "Proyectos", "FolderMultiple"),
            new(PageKey.Tasks, "Tareas", "CheckboxMarked", IsImplemented: false),
            new(PageKey.FollowUps, "Seguimientos", "ClockAlert"),
            new(PageKey.Calendar, "Calendario", "Calendar"),
            new(PageKey.People, "Personas", "AccountMultiple"),
            new(PageKey.Companies, "Empresas", "OfficeBuilding"),
            new(PageKey.Tags, "Etiquetas", "TagMultiple"),
            new(PageKey.Files, "Archivos", "Paperclip"),
            new(PageKey.Activity, "Actividad", "History"),
            new(PageKey.Statistics, "Estadísticas", "ChartBox"),
            new(PageKey.Templates, "Plantillas", "FileTreeOutline"),
            new(PageKey.Archived, "Archivados", "Archive", IsImplemented: false),
            new(PageKey.Settings, "Configuración", "Cog"),
        ];

        Palette.RequestClose += (_, _) => IsCommandPaletteOpen = false;
        Search.RequestClose += (_, _) => IsSearchOpen = false;

        _navigation.Navigated += OnNavigated;
        _navigation.NavigateTo(PageKey.Dashboard);
        _ = Notifications.RefreshCountAsync();
    }

    public ObservableCollection<NavigationItem> Items { get; }

    public CommandPaletteViewModel Palette { get; }

    public SearchViewModel Search { get; }

    public NotificationCenterViewModel Notifications { get; }

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
        IsCommandPaletteOpen = false;
        IsSearchOpen = false;
        OnPropertyChanged(nameof(CanGoBack));
        _ = Notifications.RefreshCountAsync();

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
        Inbox.InboxViewModel => PageKey.Inbox,
        ProjectsViewModel => PageKey.Projects,
        ProjectDetailViewModel => PageKey.Projects,
        Tasks.TaskDetailViewModel => PageKey.Projects,
        FollowUps.FollowUpsViewModel => PageKey.FollowUps,
        Calendar.CalendarViewModel => PageKey.Calendar,
        People.PeopleViewModel => PageKey.People,
        People.PersonDetailViewModel => PageKey.People,
        Companies.CompaniesViewModel => PageKey.Companies,
        Companies.CompanyDetailViewModel => PageKey.Companies,
        Tags.TagsViewModel => PageKey.Tags,
        Files.FilesViewModel => PageKey.Files,
        Statistics.StatisticsViewModel => PageKey.Statistics,
        Templates.TemplatesViewModel => PageKey.Templates,
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

    [RelayCommand]
    private void OpenCommandPalette()
    {
        IsSearchOpen = false;
        Palette.Reset();
        IsCommandPaletteOpen = true;
    }

    [RelayCommand]
    private void OpenSearch()
    {
        IsCommandPaletteOpen = false;
        Search.Reset();
        IsSearchOpen = true;
    }

    [RelayCommand]
    private void CloseOverlays()
    {
        IsCommandPaletteOpen = false;
        IsSearchOpen = false;
    }

    [RelayCommand]
    private void NewProject()
        => _navigation.NavigateTo<ProjectsViewModel>(vm => vm.IsCreatePanelOpen = true);

    [RelayCommand]
    private void QuickCapture() => _quickCapture.Show();

    private void UpdateThemeGlyph(ThemeMode mode) => ThemeGlyph = mode switch
    {
        ThemeMode.Light => "☀",
        ThemeMode.Dark => "☾",
        _ => "◐",
    };
}
