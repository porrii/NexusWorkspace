using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IUnitOfWorkRunner _unitOfWork;
    private readonly IClock _clock;
    private readonly INavigationService _navigation;
    private readonly ISettingsStore _settings;
    private bool _loadingWidgetPrefs;

    [ObservableProperty]
    private string _greeting = string.Empty;

    [ObservableProperty]
    private string _summaryLine = string.Empty;

    [ObservableProperty]
    private int _openTaskCount;

    [ObservableProperty]
    private int _criticalTaskCount;

    [ObservableProperty]
    private int _overdueTaskCount;

    [ObservableProperty]
    private int _waitingTaskCount;

    [ObservableProperty]
    private int _inboxCount;

    [ObservableProperty]
    private bool _anyActiveProjects;

    [ObservableProperty]
    private bool _anyRecentActivity;

    [ObservableProperty]
    private bool _anyUpcoming;

    [ObservableProperty]
    private bool _isCustomizing;

    [ObservableProperty]
    private string _captureText = string.Empty;

    [ObservableProperty]
    private bool _showTiles = true;

    [ObservableProperty]
    private bool _showQuickCapture = true;

    [ObservableProperty]
    private bool _showActiveProjects = true;

    [ObservableProperty]
    private bool _showRecentActivity = true;

    [ObservableProperty]
    private bool _showUpcoming = true;

    public DashboardViewModel(
        IUnitOfWorkRunner unitOfWork,
        IClock clock,
        INavigationService navigation,
        ISettingsStore settings)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _navigation = navigation;
        _settings = settings;

        _loadingWidgetPrefs = true;
        var widgets = settings.Current.DashboardWidgets;
        ShowTiles = widgets.GetValueOrDefault("tiles", true);
        ShowQuickCapture = widgets.GetValueOrDefault("quickCapture", true);
        ShowActiveProjects = widgets.GetValueOrDefault("activeProjects", true);
        ShowRecentActivity = widgets.GetValueOrDefault("recentActivity", true);
        ShowUpcoming = widgets.GetValueOrDefault("upcoming", true);
        _loadingWidgetPrefs = false;
    }

    public ObservableCollection<ProjectListItem> ActiveProjects { get; } = [];

    public ObservableCollection<ActivityEntry> RecentActivity { get; } = [];

    public ObservableCollection<WorkTaskListItem> UpcomingTasks { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var (projects, openTasks, recent, inbox) = await _unitOfWork.RunAsync(async (sp, ct) =>
            {
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();
                var inboxReads = sp.GetRequiredService<InboxReadService>();

                var activeProjects = await projectReads.GetListAsync(ProjectListScope.Active, null, ct);
                var open = await taskReads.GetOpenAcrossWorkspaceAsync(ct);
                var recentActivity = await activityReads.GetRecentAsync(12, ct);
                var inboxCount = await inboxReads.CountPendingAsync(ct);
                return (activeProjects, open, recentActivity, inboxCount);
            });

            var today = DateTime.UtcNow.Date;

            OpenTaskCount = openTasks.Count;
            CriticalTaskCount = openTasks.Count(t => t.Priority == Priority.Critical);
            OverdueTaskCount = openTasks.Count(t => t.DueDateUtc is { } due && due.Date < today);
            WaitingTaskCount = openTasks.Count(t => t.IsWaiting);
            InboxCount = inbox;

            ActiveProjects.Reset(projects.Take(6));
            RecentActivity.Reset(recent);
            UpcomingTasks.Reset(openTasks
                .Where(t => t.DueDateUtc is not null)
                .OrderBy(t => t.DueDateUtc)
                .Take(6));

            AnyActiveProjects = ActiveProjects.Count > 0;
            AnyRecentActivity = RecentActivity.Count > 0;
            AnyUpcoming = UpcomingTasks.Count > 0;

            Greeting = BuildGreeting();
            SummaryLine = BuildSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar el panel: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenProject(ProjectListItem? project)
    {
        if (project is null)
        {
            return;
        }

        _navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(project.Id));
    }

    [RelayCommand]
    private void OpenInbox() => _navigation.NavigateTo(PageKey.Inbox);

    [RelayCommand]
    private void ToggleCustomize() => IsCustomizing = !IsCustomizing;

    [RelayCommand]
    private async Task CaptureAsync()
    {
        var text = CaptureText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CaptureText = string.Empty;
        await _unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<InboxService>().CaptureAsync(text, null, ct));
        await RefreshAsync();
    }

    partial void OnShowTilesChanged(bool value) => PersistWidget("tiles", value);

    partial void OnShowQuickCaptureChanged(bool value) => PersistWidget("quickCapture", value);

    partial void OnShowActiveProjectsChanged(bool value) => PersistWidget("activeProjects", value);

    partial void OnShowRecentActivityChanged(bool value) => PersistWidget("recentActivity", value);

    partial void OnShowUpcomingChanged(bool value) => PersistWidget("upcoming", value);

    private void PersistWidget(string key, bool value)
    {
        if (_loadingWidgetPrefs)
        {
            return;
        }

        _ = _settings.UpdateAsync(s => s.DashboardWidgets[key] = value);
    }

    private string BuildGreeting()
    {
        var hour = _clock.UtcNow.ToLocalTime().Hour;
        return hour is >= 6 and < 13 ? "Buenos días"
            : hour is >= 13 and < 21 ? "Buenas tardes"
            : "Buenas noches";
    }

    private string BuildSummary()
    {
        var parts = new List<string>();
        if (OpenTaskCount > 0)
        {
            parts.Add($"{OpenTaskCount} tareas abiertas");
        }

        if (CriticalTaskCount > 0)
        {
            parts.Add($"{CriticalTaskCount} críticas");
        }

        if (OverdueTaskCount > 0)
        {
            parts.Add($"{OverdueTaskCount} vencidas");
        }

        if (WaitingTaskCount > 0)
        {
            parts.Add($"{WaitingTaskCount} esperando respuesta");
        }

        if (InboxCount > 0)
        {
            parts.Add($"{InboxCount} en Inbox");
        }

        return parts.Count == 0
            ? "Sin tareas pendientes. Todo al día."
            : string.Join(" · ", parts) + ".";
    }
}
