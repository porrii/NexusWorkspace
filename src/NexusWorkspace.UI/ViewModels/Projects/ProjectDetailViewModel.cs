using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Application.Templates;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Shared;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Projects;

public partial class ProjectDetailViewModel(
    IUnitOfWorkRunner unitOfWork,
    INavigationService navigation,
    IPlatformLauncher launcher) : ViewModelBase
{
    private Guid _projectId;

    private static readonly WorkTaskStatus[] BoardOrder =
    [
        WorkTaskStatus.Pending, WorkTaskStatus.InProgress, WorkTaskStatus.WaitingClient,
        WorkTaskStatus.WaitingProvider, WorkTaskStatus.Blocked, WorkTaskStatus.Finished, WorkTaskStatus.Cancelled,
    ];

    [ObservableProperty]
    private ProjectDetail? _header;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _showFinishedTasks;

    [ObservableProperty]
    private bool _isCreateTaskPanelOpen;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private Priority _newTaskPriority = Priority.Medium;

    [ObservableProperty]
    private bool _isFollowUpPanelOpen;

    [ObservableProperty]
    private string _followUpSubject = string.Empty;

    [ObservableProperty]
    private string _followUpWaitingOn = string.Empty;

    [ObservableProperty]
    private bool _isReminderPanelOpen;

    [ObservableProperty]
    private string _newReminderText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _newReminderDate = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty]
    private bool _isTemplatePanelOpen;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string? _exportMessage;

    public ObservableCollection<WorkTaskListItem> Tasks { get; } = [];

    public ObservableCollection<ActivityEntry> Timeline { get; } = [];

    public ObservableCollection<FollowUpListItem> FollowUps { get; } = [];

    public ObservableCollection<ReminderView> Reminders { get; } = [];

    public ObservableCollection<ProjectStatus> AvailableStatuses { get; } = [];

    public ObservableCollection<KanbanColumn> Board { get; } = [];

    public ObservableCollection<TimelineDay> TimelineDays { get; } = [];

    public AttachmentsSectionViewModel Attachments { get; } = new(unitOfWork, launcher);

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    public void Load(Guid projectId)
    {
        _projectId = projectId;
        _ = RefreshAsync();
    }

    public override Task OnActivatedAsync()
        => _projectId == Guid.Empty ? Task.CompletedTask : RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_projectId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var scope = ShowFinishedTasks ? TaskListScope.All : TaskListScope.Open;

            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();
                var followUpReads = sp.GetRequiredService<FollowUpReadService>();
                var reminderReads = sp.GetRequiredService<ReminderReadService>();

                var header = await projectReads.GetDetailAsync(_projectId, ct);
                var taskList = await taskReads.GetForProjectAsync(_projectId, scope, ct);
                var boardTasks = await taskReads.GetForProjectAsync(_projectId, TaskListScope.All, ct);
                var events = await activityReads.GetForProjectAsync(_projectId, 200, ct);
                var followUps = await followUpReads.GetForEntityAsync(EntityKind.Project, _projectId, ct);
                var reminders = await reminderReads.GetForEntityAsync(EntityKind.Project, _projectId, ct);
                return (header, taskList, boardTasks, events, followUps, reminders);
            });

            var detail = data.header;
            if (detail is null)
            {
                ErrorMessage = "El proyecto ya no existe.";
                return;
            }

            Header = detail;
            Tasks.Reset(data.taskList);
            Timeline.Reset(data.events);
            FollowUps.Reset(data.followUps);
            Reminders.Reset(data.reminders);

            AvailableStatuses.Reset(
                new[] { detail.Status }
                    .Concat(ProjectStateMachine.NextStates(detail.Status))
                    .Distinct());

            BuildBoard(data.boardTasks);
            BuildTimeline(data.events);

            Attachments.Bind(EntityKind.Project, _projectId, _projectId, RefreshAsync);
            await Attachments.LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar el proyecto: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnShowFinishedTasksChanged(bool value) => _ = RefreshAsync();

    private void BuildBoard(IReadOnlyList<WorkTaskListItem> tasks)
    {
        var byStatus = tasks.Where(t => !t.IsArchived).ToLookup(t => t.Status);
        Board.Reset(BoardOrder.Select(status =>
        {
            var column = new KanbanColumn(status, DisplayNames.Of(status));
            foreach (var card in byStatus[status].OrderBy(t => t.SortKey))
            {
                column.Cards.Add(card);
            }

            return column;
        }));
    }

    private void BuildTimeline(IReadOnlyList<ActivityEntry> events)
    {
        var groups = events
            .GroupBy(e => DateOnly.FromDateTime(DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc).ToLocalTime()))
            .OrderByDescending(g => g.Key);

        var days = new List<TimelineDay>();
        foreach (var group in groups)
        {
            var day = new TimelineDay(group.Key, DayLabel(group.Key));
            foreach (var entry in group.OrderByDescending(e => e.OccurredAtUtc))
            {
                day.Events.Add(entry);
            }

            days.Add(day);
        }

        TimelineDays.Reset(days);
    }

    private static string DayLabel(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (date == today)
        {
            return "Hoy";
        }

        if (date == today.AddDays(-1))
        {
            return "Ayer";
        }

        return date.ToString("dddd d 'de' MMMM yyyy");
    }

    [RelayCommand]
    private async Task MoveCardAsync((WorkTaskListItem Card, WorkTaskStatus Target) move)
    {
        if (move.Card is null || move.Card.Status == move.Target)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().ChangeStatusAsync(move.Card.Id, move.Target, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenTask(WorkTaskListItem? task)
    {
        if (task is null)
        {
            return;
        }

        navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(task.Id));
    }

    [RelayCommand]
    private void ToggleCreateTaskPanel()
    {
        IsCreateTaskPanelOpen = !IsCreateTaskPanelOpen;
        if (!IsCreateTaskPanelOpen)
        {
            NewTaskTitle = string.Empty;
            NewTaskPriority = Priority.Medium;
        }
    }

    [RelayCommand]
    private async Task CreateTaskAsync()
    {
        var title = NewTaskTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorMessage = "Escribe un título para la tarea.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().CreateAsync(new CreateWorkTaskRequest
                {
                    ProjectId = _projectId,
                    Title = title,
                    Priority = NewTaskPriority,
                }, ct));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            NewTaskTitle = string.Empty;
            IsCreateTaskPanelOpen = false;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo crear la tarea: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(ProjectStatus status)
    {
        if (Header is null || Header.Status == status)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ProjectService>().ChangeStatusAsync(_projectId, status, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (Header is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
        {
            var service = sp.GetRequiredService<ProjectService>();
            return Header.IsArchived
                ? service.UnarchiveAsync(_projectId, ct)
                : service.ArchiveAsync(_projectId, ct);
        });

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleFollowUpPanel()
    {
        IsFollowUpPanelOpen = !IsFollowUpPanelOpen;
        if (!IsFollowUpPanelOpen)
        {
            FollowUpSubject = string.Empty;
            FollowUpWaitingOn = string.Empty;
        }
    }

    [RelayCommand]
    private async Task StartFollowUpAsync()
    {
        var subject = FollowUpSubject?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            ErrorMessage = "Indica qué estás esperando.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().StartAsync(new StartFollowUpRequest
            {
                TargetKind = EntityKind.Project,
                TargetId = _projectId,
                Subject = subject,
                WaitingOnLabel = string.IsNullOrWhiteSpace(FollowUpWaitingOn) ? null : FollowUpWaitingOn.Trim(),
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        FollowUpSubject = string.Empty;
        FollowUpWaitingOn = string.Empty;
        IsFollowUpPanelOpen = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task SendFollowUpReminderAsync(FollowUpListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<FollowUpService>().SendReminderAsync(item.Id, null, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ResolveFollowUpAsync(FollowUpListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<FollowUpService>().ResolveAsync(
            new ResolveFollowUpRequest { Id = item.Id, State = FollowUpState.Answered }, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleReminderPanel()
    {
        IsReminderPanelOpen = !IsReminderPanelOpen;
        if (!IsReminderPanelOpen)
        {
            NewReminderText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task AddReminderAsync()
    {
        var text = NewReminderText?.Trim();
        if (string.IsNullOrWhiteSpace(text) || NewReminderDate is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ReminderService>().CreateAsync(new CreateReminderRequest
            {
                Text = text,
                RemindAtUtc = NewReminderDate.Value.UtcDateTime,
                TargetKind = EntityKind.Project,
                TargetId = _projectId,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        NewReminderText = string.Empty;
        IsReminderPanelOpen = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CompleteReminderAsync(ReminderView? reminder)
    {
        if (reminder is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<ReminderService>().CompleteAsync(reminder.Id, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleTemplatePanel()
    {
        IsTemplatePanelOpen = !IsTemplatePanelOpen;
        if (IsTemplatePanelOpen && Header is not null && string.IsNullOrWhiteSpace(TemplateName))
        {
            TemplateName = Header.Name;
        }
    }

    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        if (Header is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(TemplateName) ? Header.Name : TemplateName.Trim();
        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TemplateService>().CreateFromProjectAsync(_projectId, name, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        IsTemplatePanelOpen = false;
        TemplateName = string.Empty;
        ExportMessage = $"Plantilla «{name}» guardada.";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ExportReportAsync(string format)
    {
        var kind = format?.ToLowerInvariant() switch
        {
            "json" => Application.Export.ExportFormat.Json,
            "excel" => Application.Export.ExportFormat.Excel,
            "pdf" => Application.Export.ExportFormat.Pdf,
            _ => Application.Export.ExportFormat.Csv,
        };

        try
        {
            var path = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var data = await sp.GetRequiredService<Application.Export.ReportDataService>().BuildProjectReportAsync(_projectId, ct);
                if (data is null)
                {
                    return null;
                }

                return await sp.GetRequiredService<Application.Export.IReportExporter>().ExportProjectReportAsync(data, kind, ct);
            });

            if (path is null)
            {
                ErrorMessage = "No se pudo generar el informe.";
                return;
            }

            ExportMessage = $"Informe guardado en {path}";
            launcher.RevealInFolder(path);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo exportar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Back() => navigation.GoBack();
}
