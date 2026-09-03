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
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Projects;

public partial class ProjectDetailViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private Guid _projectId;

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

    public ObservableCollection<WorkTaskListItem> Tasks { get; } = [];

    public ObservableCollection<ActivityEntry> Timeline { get; } = [];

    public ObservableCollection<ProjectStatus> AvailableStatuses { get; } = [];

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

            var (detail, tasks, timeline) = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var taskReads = sp.GetRequiredService<WorkTaskReadService>();
                var activityReads = sp.GetRequiredService<ActivityReadService>();

                var header = await projectReads.GetDetailAsync(_projectId, ct);
                var taskList = await taskReads.GetForProjectAsync(_projectId, scope, ct);
                var events = await activityReads.GetForProjectAsync(_projectId, 200, ct);
                return (header, taskList, events);
            });

            if (detail is null)
            {
                ErrorMessage = "El proyecto ya no existe.";
                return;
            }

            Header = detail;
            Tasks.Reset(tasks);
            Timeline.Reset(timeline);

            AvailableStatuses.Reset(
                new[] { detail.Status }
                    .Concat(ProjectStateMachine.NextStates(detail.Status))
                    .Distinct());
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
    private void Back() => navigation.GoBack();
}
