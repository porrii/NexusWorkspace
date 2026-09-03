using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.ViewModels.Dashboard;

public partial class DashboardViewModel(IUnitOfWorkRunner unitOfWork, IClock clock, INavigationService navigation)
    : ViewModelBase
{
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
    private bool _hasContent;

    [ObservableProperty]
    private bool _anyActiveProjects;

    [ObservableProperty]
    private bool _anyRecentActivity;

    [ObservableProperty]
    private bool _anyUpcoming;

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
            var (projects, openTasks, recent) = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();

                var activeProjects = await projectReads.GetListAsync(ProjectListScope.Active, null, ct);
                var open = await taskReads.GetOpenAcrossWorkspaceAsync(ct);
                var recentActivity = await activityReads.GetRecentAsync(12, ct);
                return (activeProjects, open, recentActivity);
            });

            var today = DateTime.UtcNow.Date;

            OpenTaskCount = openTasks.Count;
            CriticalTaskCount = openTasks.Count(t => t.Priority == Priority.Critical);
            OverdueTaskCount = openTasks.Count(t => t.DueDateUtc is { } due && due.Date < today);
            WaitingTaskCount = openTasks.Count(t => t.IsWaiting);

            ActiveProjects.Reset(projects.Take(6));
            RecentActivity.Reset(recent);
            UpcomingTasks.Reset(openTasks
                .Where(t => t.DueDateUtc is not null)
                .OrderBy(t => t.DueDateUtc)
                .Take(6));

            Greeting = BuildGreeting();
            SummaryLine = BuildSummary();
            HasContent = projects.Count > 0 || openTasks.Count > 0 || recent.Count > 0;
            AnyActiveProjects = ActiveProjects.Count > 0;
            AnyRecentActivity = RecentActivity.Count > 0;
            AnyUpcoming = UpcomingTasks.Count > 0;
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

        navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(project.Id));
    }

    private string BuildGreeting()
    {
        var hour = clock.UtcNow.ToLocalTime().Hour;
        var part = hour is >= 6 and < 13 ? "Buenos días"
            : hour is >= 13 and < 21 ? "Buenas tardes"
            : "Buenas noches";
        return part;
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

        return parts.Count == 0
            ? "Sin tareas pendientes. Todo al día."
            : string.Join(" · ", parts) + ".";
    }
}
