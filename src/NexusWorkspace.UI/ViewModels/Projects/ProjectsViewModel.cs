using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.Projects;

public partial class ProjectsViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private bool _isCreatePanelOpen;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newProjectDescription = string.Empty;

    [ObservableProperty]
    private Priority _newProjectPriority = Priority.Medium;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<ProjectListItem> Projects { get; } = [];

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnShowArchivedChanged(bool value) => _ = RefreshAsync();

    partial void OnSearchTextChanged(string? value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var scope = ShowArchived ? ProjectListScope.Archived : ProjectListScope.Active;
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ProjectReadService>().GetListAsync(scope, SearchText, ct));

            Projects.Reset(rows);
            IsEmpty = Projects.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar los proyectos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleCreatePanel()
    {
        IsCreatePanelOpen = !IsCreatePanelOpen;
        if (!IsCreatePanelOpen)
        {
            ResetCreateForm();
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

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        var name = NewProjectName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Escribe un nombre para el proyecto.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ProjectService>().CreateAsync(new CreateProjectRequest
                {
                    Name = name,
                    Description = NewProjectDescription,
                    Priority = NewProjectPriority,
                }, ct));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            var newId = result.Value;
            IsCreatePanelOpen = false;
            ResetCreateForm();
            await RefreshAsync();
            navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(newId));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo crear el proyecto: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetCreateForm()
    {
        NewProjectName = string.Empty;
        NewProjectDescription = string.Empty;
        NewProjectPriority = Priority.Medium;
    }
}
