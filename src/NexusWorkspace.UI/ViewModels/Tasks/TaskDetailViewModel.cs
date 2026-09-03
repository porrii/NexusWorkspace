using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.QuickActions;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Tasks;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.Tasks;

public partial class TaskDetailViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private Guid _taskId;

    [ObservableProperty]
    private WorkTaskDetail? _detail;

    [ObservableProperty]
    private string _newSubTaskTitle = string.Empty;

    [ObservableProperty]
    private string _newChecklistText = string.Empty;

    [ObservableProperty]
    private string _newCommentBody = string.Empty;

    [ObservableProperty]
    private string _quickActionNote = string.Empty;

    public ObservableCollection<SubTaskNode> SubTasks { get; } = [];

    public ObservableCollection<ChecklistItemView> Checklist { get; } = [];

    public ObservableCollection<TaskCommentView> Comments { get; } = [];

    public ObservableCollection<ActivityEntry> History { get; } = [];

    public ObservableCollection<WorkTaskStatus> AvailableStatuses { get; } = [];

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
            var (detail, history) = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();
                var d = await taskReads.GetDetailAsync(_taskId, ct);
                var h = await activityReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, 200, ct);
                return (d, h);
            });

            if (detail is null)
            {
                ErrorMessage = "La tarea ya no existe.";
                return;
            }

            Detail = detail;
            SubTasks.Reset(detail.SubTasks);
            Checklist.Reset(detail.Checklist);
            Comments.Reset(detail.Comments);
            History.Reset(history);

            AvailableStatuses.Reset(
                new[] { detail.Status }
                    .Concat(WorkTaskStateMachine.NextStates(detail.Status))
                    .Distinct());

            OnPropertyChanged(nameof(SubTaskProgress));
            OnPropertyChanged(nameof(ChecklistProgress));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la tarea: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
