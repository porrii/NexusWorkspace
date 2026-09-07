using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Reminders;
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
    private bool _showOnboarding;

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

    [ObservableProperty]
    private bool _showWaitingOn = true;

    [ObservableProperty]
    private bool _showReminders = true;

    [ObservableProperty]
    private string _reminderText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _reminderDate = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty]
    private bool _anyWaitingOn;

    [ObservableProperty]
    private bool _anyReminders;

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
        ShowWaitingOn = widgets.GetValueOrDefault("waitingOn", true);
        ShowReminders = widgets.GetValueOrDefault("reminders", true);
        _loadingWidgetPrefs = false;
    }

    public ObservableCollection<ProjectListItem> ActiveProjects { get; } = [];

    public ObservableCollection<ActivityEntry> RecentActivity { get; } = [];

    public ObservableCollection<WorkTaskListItem> UpcomingTasks { get; } = [];

    public ObservableCollection<FollowUpListItem> WaitingOn { get; } = [];

    public ObservableCollection<ReminderView> UpcomingReminders { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var data = await _unitOfWork.RunAsync(async (sp, ct) =>
            {
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();
                var inboxReads = sp.GetRequiredService<InboxReadService>();
                var followUpReads = sp.GetRequiredService<FollowUpReadService>();
                var reminderReads = sp.GetRequiredService<ReminderReadService>();

                var activeProjects = await projectReads.GetListAsync(ProjectListScope.Active, null, ct);
                var summary = await taskReads.GetOpenSummaryAsync(ct);
                var upcoming = await taskReads.GetOpenAcrossWorkspaceAsync(ct);
                var recentActivity = await activityReads.GetRecentAsync(12, ct);
                var inboxCount = await inboxReads.CountPendingAsync(ct);
                var waiting = await followUpReads.GetListAsync(FollowUpScope.Open, ct);
                var reminders = await reminderReads.GetPendingAsync(ct);
                return (activeProjects, summary, upcoming, recentActivity, inboxCount, waiting, reminders);
            });

            OpenTaskCount = data.summary.Open;
            CriticalTaskCount = data.summary.Critical;
            OverdueTaskCount = data.summary.Overdue;
            WaitingTaskCount = data.summary.Waiting;
            InboxCount = data.inboxCount;

            ActiveProjects.Reset(data.activeProjects.Take(6));
            RecentActivity.Reset(data.recentActivity);
            UpcomingTasks.Reset(data.upcoming
                .Where(t => t.DueDateUtc is not null)
                .OrderBy(t => t.DueDateUtc)
                .Take(6));
            WaitingOn.Reset(data.waiting.Take(6));
            UpcomingReminders.Reset(data.reminders.Take(6));

            AnyActiveProjects = ActiveProjects.Count > 0;
            AnyRecentActivity = RecentActivity.Count > 0;
            AnyUpcoming = UpcomingTasks.Count > 0;
            AnyWaitingOn = WaitingOn.Count > 0;
            AnyReminders = UpcomingReminders.Count > 0;

            ShowOnboarding = data.activeProjects.Count == 0 && data.summary.Open == 0
                             && data.inboxCount == 0 && data.recentActivity.Count == 0;

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
    private void OpenFollowUps() => _navigation.NavigateTo(PageKey.FollowUps);

    [RelayCommand]
    private void OpenCalendar() => _navigation.NavigateTo(PageKey.Calendar);

    [RelayCommand]
    private void StartFirstProject()
        => _navigation.NavigateTo<ProjectsViewModel>(vm => vm.IsCreatePanelOpen = true);

    [RelayCommand]
    private void OpenSettingsForImport() => _navigation.NavigateTo(PageKey.Settings);

    [RelayCommand]
    private async Task LoadDemoWorkspaceAsync()
    {
        try
        {
            await _unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<IDemoDataService>().SeedAsync(ct));
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la demo: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleCustomize() => IsCustomizing = !IsCustomizing;

    [RelayCommand]
    private async Task SendFollowUpReminderAsync(FollowUpListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await _unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().SendReminderAsync(item.Id, null, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddReminderAsync()
    {
        var text = ReminderText?.Trim();
        if (string.IsNullOrWhiteSpace(text) || ReminderDate is null)
        {
            return;
        }

        var result = await _unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ReminderService>().CreateAsync(new CreateReminderRequest
            {
                Text = text,
                RemindAtUtc = ReminderDate.Value.UtcDateTime,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ReminderText = string.Empty;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CompleteReminderAsync(ReminderView? reminder)
    {
        if (reminder is null)
        {
            return;
        }

        await _unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<ReminderService>().CompleteAsync(reminder.Id, ct));
        await RefreshAsync();
    }

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

    partial void OnShowWaitingOnChanged(bool value) => PersistWidget("waitingOn", value);

    partial void OnShowRemindersChanged(bool value) => PersistWidget("reminders", value);

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
