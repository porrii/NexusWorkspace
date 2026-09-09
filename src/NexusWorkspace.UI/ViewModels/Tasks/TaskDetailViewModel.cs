using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.QuickActions;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Tasks;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Shared;

namespace NexusWorkspace.UI.ViewModels.Tasks;

public partial class TaskDetailViewModel(
    IUnitOfWorkRunner unitOfWork,
    INavigationService navigation,
    IPlatformLauncher launcher) : ViewModelBase
{
    private Guid _taskId;

    // While RefreshAsync rebuilds AvailableStatuses and re-selects the current
    // status, OnSelectedStatusChanged must not treat that as a user change.
    private bool _suppressStatusChange;

    [ObservableProperty]
    private WorkTaskDetail? _detail;

    [ObservableProperty]
    private WorkTaskStatus? _selectedStatus;

    [ObservableProperty]
    private string _newSubTaskTitle = string.Empty;

    [ObservableProperty]
    private string _newChecklistText = string.Empty;

    [ObservableProperty]
    private string _newCommentBody = string.Empty;

    [ObservableProperty]
    private string _quickActionNote = string.Empty;

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
    private string? _templateMessage;

    public ObservableCollection<FollowUpListItem> FollowUps { get; } = [];

    public ObservableCollection<ReminderView> Reminders { get; } = [];

    public ObservableCollection<SubTaskNode> SubTasks { get; } = [];

    public ObservableCollection<ChecklistItemView> Checklist { get; } = [];

    public ObservableCollection<TaskCommentView> Comments { get; } = [];

    public ObservableCollection<ActivityEntry> History { get; } = [];

    public ObservableCollection<WorkTaskStatus> AvailableStatuses { get; } = [];

    public AttachmentsSectionViewModel Attachments { get; } = new(unitOfWork, launcher);

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    public IReadOnlyList<QuickActionDescriptor> QuickActions => QuickActionCatalog.All;

    public int SubTaskProgress => SubTasks.Count == 0
        ? 0
        : (int)Math.Round(100.0 * SubTasks.Count(s => s.IsDone) / SubTasks.Count);

    public int ChecklistProgress => Checklist.Count == 0
        ? 0
        : (int)Math.Round(100.0 * Checklist.Count(c => c.IsChecked) / Checklist.Count);

    public void Load(Guid taskId)
    {
        _taskId = taskId;
        _ = RefreshAsync();
    }

    public override Task OnActivatedAsync()
        => _taskId == Guid.Empty ? Task.CompletedTask : RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_taskId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();
                var followUpReads = sp.GetRequiredService<FollowUpReadService>();
                var reminderReads = sp.GetRequiredService<ReminderReadService>();
                var d = await taskReads.GetDetailAsync(_taskId, ct);
                var h = await activityReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, 200, ct);
                var f = await followUpReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, ct);
                var r = await reminderReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, ct);
                return (d, h, f, r);
            });

            var detail = data.d;
            if (detail is null)
            {
                ErrorMessage = "La tarea ya no existe.";
                return;
            }

            // Rebuild the status list and re-select it BEFORE assigning Detail,
            // with the selection cleared during the swap: binding the ComboBox to
            // an empty list first left the field blank, and mutating a bound
            // collection while an item is selected throws inside Avalonia's
            // selection model (ArgumentOutOfRangeException, "index").
            _suppressStatusChange = true;
            SelectedStatus = null;
            AvailableStatuses.Reset(
                new[] { detail.Status }
                    .Concat(WorkTaskStateMachine.NextStates(detail.Status))
                    .Distinct());
            Detail = detail;
            SelectedStatus = detail.Status;
            _suppressStatusChange = false;

            SubTasks.Reset(detail.SubTasks);
            Checklist.Reset(detail.Checklist);
            Comments.Reset(detail.Comments);
            History.Reset(data.h);
            FollowUps.Reset(data.f);
            Reminders.Reset(data.r);

            Attachments.Bind(EntityKind.WorkTask, _taskId, detail.ProjectId, RefreshAsync);
            await Attachments.LoadAsync();

            OnPropertyChanged(nameof(SubTaskProgress));
            OnPropertyChanged(nameof(ChecklistProgress));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la tarea: {ex.Message}";
        }
        finally
        {
            _suppressStatusChange = false;
            IsBusy = false;
        }
    }

    partial void OnSelectedStatusChanged(WorkTaskStatus? value)
    {
        if (_suppressStatusChange || value is null || Detail is null || Detail.Status == value.Value)
        {
            return;
        }

        ChangeStatusCommand.Execute(value.Value);
    }

    [RelayCommand]
    private Task ChangeStatusAsync(WorkTaskStatus status)
        => Detail is null || Detail.Status == status
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().ChangeStatusAsync(_taskId, status, ct));

    [RelayCommand]
    private Task ChangePriorityAsync(Priority priority)
        => Detail is null || Detail.Priority == priority
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().UpdateDetailsAsync(new UpdateWorkTaskDetailsRequest
                {
                    Id = _taskId,
                    Priority = priority,
                }, ct));

    [RelayCommand]
    private async Task AddSubTaskAsync()
    {
        var title = NewSubTaskTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        NewSubTaskTitle = string.Empty;
        await RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().AddSubTaskAsync(_taskId, title, null, ct));
    }

    [RelayCommand]
    private Task ToggleSubTaskAsync(SubTaskNode? node)
        => node is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().SetSubTaskDoneAsync(node.Id, !node.IsDone, ct));

    [RelayCommand]
    private async Task AddChecklistItemAsync()
    {
        var text = NewChecklistText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        NewChecklistText = string.Empty;
        await RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().AddChecklistItemAsync(_taskId, text, ct));
    }

    [RelayCommand]
    private Task ToggleChecklistItemAsync(ChecklistItemView? item)
        => item is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().SetChecklistItemCheckedAsync(item.Id, !item.IsChecked, ct));

    [RelayCommand]
    private async Task AddCommentAsync()
    {
        var body = NewCommentBody?.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        NewCommentBody = string.Empty;
        await RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().AddCommentAsync(_taskId, body, ct));
    }

    [RelayCommand]
    private async Task ExecuteQuickActionAsync(QuickActionDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return;
        }

        var note = string.IsNullOrWhiteSpace(QuickActionNote) ? null : QuickActionNote.Trim();
        QuickActionNote = string.Empty;
        await RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().ExecuteQuickActionAsync(_taskId, descriptor.Kind, note, ct));
    }

    [RelayCommand]
    private Task ArchiveAsync()
        => Detail is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
            {
                var service = sp.GetRequiredService<WorkTaskService>();
                return Detail.IsArchived
                    ? service.UnarchiveAsync(_taskId, ct)
                    : service.ArchiveAsync(_taskId, ct);
            });

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
                TargetKind = EntityKind.WorkTask,
                TargetId = _taskId,
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
    private Task SendFollowUpReminderAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().SendReminderAsync(item.Id, null, ct));

    [RelayCommand]
    private Task ResolveFollowUpAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().ResolveAsync(
                new ResolveFollowUpRequest { Id = item.Id, State = FollowUpState.Answered }, ct));

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
                TargetKind = EntityKind.WorkTask,
                TargetId = _taskId,
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
    private Task CompleteReminderAsync(ReminderView? reminder)
        => reminder is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<ReminderService>().CompleteAsync(reminder.Id, ct));

    [RelayCommand]
    private void ToggleTemplatePanel()
    {
        IsTemplatePanelOpen = !IsTemplatePanelOpen;
        if (IsTemplatePanelOpen && Detail is not null && string.IsNullOrWhiteSpace(TemplateName))
        {
            TemplateName = Detail.Title;
        }
    }

    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        if (Detail is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(TemplateName) ? Detail.Title : TemplateName.Trim();
        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<Application.Templates.TemplateService>().CreateFromTaskAsync(_taskId, name, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        IsTemplatePanelOpen = false;
        TemplateName = string.Empty;
        TemplateMessage = $"Plantilla «{name}» guardada.";
    }

    [RelayCommand]
    private void Back() => navigation.GoBack();

    private async Task RunAndRefreshAsync(Func<IServiceProvider, CancellationToken, Task<Application.Common.Result>> operation)
    {
        try
        {
            var result = await unitOfWork.RunAsync(operation);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task RunAndRefreshAsync<T>(Func<IServiceProvider, CancellationToken, Task<Application.Common.Result<T>>> operation)
    {
        try
        {
            var result = await unitOfWork.RunAsync(operation);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
