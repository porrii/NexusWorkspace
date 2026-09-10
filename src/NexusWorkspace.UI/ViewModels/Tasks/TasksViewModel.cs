using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Shared;

namespace NexusWorkspace.UI.ViewModels.Tasks;

/// <summary>The global "Tareas" page: every task across projects, with filters.</summary>
public partial class TasksViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private bool _loaded;
    private bool _suppressRefresh;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _openOnly = true;

    [ObservableProperty]
    private bool _overdueOnly;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ProjectOption? _selectedProject;

    [ObservableProperty]
    private PersonOption? _selectedAssignee;

    [ObservableProperty]
    private FilterOption<WorkTaskStatus>? _selectedStatus;

    [ObservableProperty]
    private FilterOption<Priority>? _selectedPriority;

    public ObservableCollection<WorkTaskListItem> Tasks { get; } = [];

    public ObservableCollection<ProjectOption> ProjectOptions { get; } = [];

    public ObservableCollection<PersonOption> AssigneeOptions { get; } = [];

    public IReadOnlyList<FilterOption<WorkTaskStatus>> StatusOptions { get; } =
    [
        new("Cualquier estado", null),
        .. Enum.GetValues<WorkTaskStatus>().Select(s => new FilterOption<WorkTaskStatus>(DisplayNames.Of(s), s)),
    ];

    public IReadOnlyList<FilterOption<Priority>> PriorityOptions { get; } =
    [
        new("Cualquier prioridad", null),
        .. Enum.GetValues<Priority>().Select(p => new FilterOption<Priority>(DisplayNames.Of(p), p)),
    ];

    public override async Task OnActivatedAsync()
    {
        if (!_loaded)
        {
            await LoadOptionsAsync();
            _loaded = true;
        }

        await RefreshAsync();
    }

    partial void OnOpenOnlyChanged(bool value) => QueueRefresh();

    partial void OnOverdueOnlyChanged(bool value) => QueueRefresh();

    partial void OnSelectedProjectChanged(ProjectOption? value) => QueueRefresh();

    partial void OnSelectedAssigneeChanged(PersonOption? value) => QueueRefresh();

    partial void OnSelectedStatusChanged(FilterOption<WorkTaskStatus>? value) => QueueRefresh();

    partial void OnSelectedPriorityChanged(FilterOption<Priority>? value) => QueueRefresh();

    private void QueueRefresh()
    {
        if (!_suppressRefresh)
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private Task ApplySearchAsync() => RefreshAsync();

    [RelayCommand]
    private void ClearFilters()
    {
        _suppressRefresh = true;
        SelectedProject = ProjectOptions.Count > 0 ? ProjectOptions[0] : null;
        SelectedAssignee = AssigneeOptions.Count > 0 ? AssigneeOptions[0] : null;
        SelectedStatus = StatusOptions[0];
        SelectedPriority = PriorityOptions[0];
        OverdueOnly = false;
        OpenOnly = true;
        SearchText = string.Empty;
        _suppressRefresh = false;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void OpenTask(WorkTaskListItem? task)
    {
        if (task is not null)
        {
            navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(task.Id));
        }
    }

    private async Task LoadOptionsAsync()
    {
        var data = await unitOfWork.RunAsync(async (sp, ct) =>
        {
            var projects = await sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.All, null, ct);
            var people = await sp.GetRequiredService<PersonReadService>().GetListAsync(PersonScope.All, null, null, ct);
            return (projects, people);
        });

        _suppressRefresh = true;
        ProjectOptions.Reset(
            new[] { new ProjectOption("Todos los proyectos", null) }
                .Concat(data.projects.Select(p => new ProjectOption(p.Name, p.Id))));
        AssigneeOptions.Reset(
            new[] { new PersonOption("Cualquier responsable", null) }
                .Concat(data.people.Select(p => new PersonOption(p.Name, p.Id))));
        SelectedProject = ProjectOptions[0];
        SelectedAssignee = AssigneeOptions[0];
        SelectedStatus = StatusOptions[0];
        SelectedPriority = PriorityOptions[0];
        _suppressRefresh = false;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var filter = new WorkTaskFilter
            {
                ProjectId = SelectedProject?.Id,
                AssigneePersonId = SelectedAssignee?.Id,
                Status = SelectedStatus?.Value,
                Priority = SelectedPriority?.Value,
                OpenOnly = OpenOnly,
                OverdueOnly = OverdueOnly,
                Text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            };

            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskReadService>().GetFilteredAsync(filter, 1000, ct));

            Tasks.Reset(rows);
            IsEmpty = Tasks.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar las tareas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
