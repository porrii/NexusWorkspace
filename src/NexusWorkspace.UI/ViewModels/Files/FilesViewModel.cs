using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Attachments;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Companies;
using NexusWorkspace.UI.ViewModels.People;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Files;

public sealed record ProjectFilterOption(Guid? Id, string Name)
{
    public static ProjectFilterOption All { get; } = new(null, "Todos los proyectos");
}

public sealed record KindFilterOption(AttachmentKind? Kind, string Name)
{
    public static KindFilterOption All { get; } = new(null, "Todos los tipos");
}

public partial class FilesViewModel(
    IUnitOfWorkRunner unitOfWork,
    INavigationService navigation,
    IPlatformLauncher launcher) : ViewModelBase
{
    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private ProjectFilterOption? _selectedProject;

    [ObservableProperty]
    private KindFilterOption? _selectedKind;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private int _totalCount;

    public ObservableCollection<AttachmentListItem> Files { get; } = [];

    public ObservableCollection<ProjectFilterOption> Projects { get; } = [];

    public IReadOnlyList<KindFilterOption> Kinds { get; } =
    [
        KindFilterOption.All,
        new(AttachmentKind.Image, "Imágenes"),
        new(AttachmentKind.Pdf, "PDF"),
        new(AttachmentKind.Document, "Documentos"),
        new(AttachmentKind.Spreadsheet, "Hojas de cálculo"),
        new(AttachmentKind.Archive, "Comprimidos"),
        new(AttachmentKind.Audio, "Audio"),
        new(AttachmentKind.Video, "Vídeo"),
        new(AttachmentKind.Text, "Texto"),
        new(AttachmentKind.Other, "Otros"),
    ];

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnSearchTextChanged(string? value) => _ = RefreshAsync();

    partial void OnSelectedProjectChanged(ProjectFilterOption? value) => _ = RefreshAsync();

    partial void OnSelectedKindChanged(KindFilterOption? value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var filter = new AttachmentFilter
            {
                ProjectId = SelectedProject?.Id,
                Kind = SelectedKind?.Kind,
                Search = SearchText,
            };

            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var files = await sp.GetRequiredService<AttachmentReadService>().GetAllAsync(filter, ct);
                var projects = await sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.All, null, ct);
                return (files, projects);
            });

            Files.Reset(data.files);
            TotalCount = Files.Count;
            IsEmpty = Files.Count == 0;

            var current = SelectedProject?.Id;
            var options = new List<ProjectFilterOption> { ProjectFilterOption.All };
            options.AddRange(data.projects.Select(p => new ProjectFilterOption(p.Id, p.Name)));
            Projects.Reset(options);
            SelectedProject = options.FirstOrDefault(o => o.Id == current) ?? ProjectFilterOption.All;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar los archivos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Open(AttachmentListItem? item)
    {
        if (item is not null)
        {
            launcher.OpenPath(item.AbsolutePath);
        }
    }

    [RelayCommand]
    private void Reveal(AttachmentListItem? item)
    {
        if (item is not null)
        {
            launcher.RevealInFolder(item.AbsolutePath);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(AttachmentListItem? item)
    {
        if (item is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<AttachmentService>().RemoveAsync(item.Id, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenOwner(AttachmentListItem? item)
    {
        if (item is null)
        {
            return;
        }

        switch (item.TargetKind)
        {
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            case EntityKind.Person:
                navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            case EntityKind.Company:
                navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            default:
                if (item.ProjectId is { } projectId)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(projectId));
                }

                break;
        }
    }
}
