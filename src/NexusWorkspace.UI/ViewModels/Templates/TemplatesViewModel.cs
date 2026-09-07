using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Templates;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Files;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.ViewModels.Templates;

public partial class TemplatesViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private ProjectFilterOption? _targetProject;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<TemplateListItem> ProjectTemplates { get; } = [];

    public ObservableCollection<TemplateListItem> TaskTemplates { get; } = [];

    public ObservableCollection<ProjectFilterOption> Projects { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var templates = await sp.GetRequiredService<TemplateReadService>().GetListAsync(null, ct);
                var projects = await sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.Active, null, ct);
                return (templates, projects);
            });

            ProjectTemplates.Reset(data.templates.Where(t => t.Kind == TemplateKind.Project));
            TaskTemplates.Reset(data.templates.Where(t => t.Kind == TemplateKind.Task));
            IsEmpty = data.templates.Count == 0;

            var current = TargetProject?.Id;
            var options = data.projects.Select(p => new ProjectFilterOption(p.Id, p.Name)).ToList();
            Projects.Reset(options);
            TargetProject = options.FirstOrDefault(o => o.Id == current) ?? options.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar las plantillas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyProjectTemplateAsync(TemplateListItem? template)
    {
        if (template is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewProjectName) ? template.Name : NewProjectName.Trim();

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TemplateService>().ApplyProjectTemplateAsync(template.Id, name, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        NewProjectName = string.Empty;
        await RefreshAsync();
        navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(result.Value));
    }

    [RelayCommand]
    private async Task ApplyTaskTemplateAsync(TemplateListItem? template)
    {
        if (template is null || TargetProject?.Id is not { } projectId)
        {
            ErrorMessage = "Elige el proyecto de destino.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TemplateService>().ApplyTaskTemplateAsync(template.Id, projectId, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await RefreshAsync();
        navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(projectId));
    }

    [RelayCommand]
    private async Task DeleteAsync(TemplateListItem? template)
    {
        if (template is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<TemplateService>().DeleteAsync(template.Id, ct));
        await RefreshAsync();
    }
}
