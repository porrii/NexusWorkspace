using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.People;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Shared;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Companies;

public partial class CompanyDetailViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private Guid _companyId;

    [ObservableProperty]
    private CompanyDetail? _header;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isEditPanelOpen;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private CompanyKind _editKind = CompanyKind.Client;

    [ObservableProperty]
    private string _editWebsite = string.Empty;

    [ObservableProperty]
    private string _editNotes = string.Empty;

    public IReadOnlyList<CompanyKind> Kinds { get; } = Enum.GetValues<CompanyKind>();

    public CommunicationComposerViewModel Composer { get; } = new();

    public RelationComposerViewModel RelationComposer { get; } = new();

    public ObservableCollection<PersonListItem> People { get; } = [];

    public ObservableCollection<ProjectListItem> Projects { get; } = [];

    public ObservableCollection<WorkTaskListItem> Tasks { get; } = [];

    public ObservableCollection<FollowUpListItem> FollowUps { get; } = [];

    public ObservableCollection<CommunicationListItem> Communications { get; } = [];

    public ObservableCollection<RelationView> Relations { get; } = [];

    public ObservableCollection<ActivityEntry> Timeline { get; } = [];

    public ObservableCollection<TagListItem> AllTags { get; } = [];

    public void Load(Guid companyId)
    {
        _companyId = companyId;
        _ = RefreshAsync();
    }

    public override Task OnActivatedAsync()
        => _companyId == Guid.Empty ? Task.CompletedTask : RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_companyId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var header = await sp.GetRequiredService<CompanyReadService>().GetDetailAsync(_companyId, ct);
                var people = await sp.GetRequiredService<PersonReadService>().GetListAsync(PersonScope.All, null, null, ct);
                var projects = await sp.GetRequiredService<ProjectReadService>().GetForCompanyAsync(_companyId, ct);
                var tasks = await sp.GetRequiredService<WorkTaskReadService>().GetForCompanyAsync(_companyId, false, ct);
                var followUps = await sp.GetRequiredService<FollowUpReadService>().GetWaitingOnCompanyAsync(_companyId, ct);
                var comms = await sp.GetRequiredService<CommunicationReadService>().GetForCompanyAsync(_companyId, 200, ct);
                var relations = await sp.GetRequiredService<RelationReadService>().GetForEntityAsync(EntityKind.Company, _companyId, ct);
                var timeline = await sp.GetRequiredService<ActivityReadService>().GetForEntityAsync(EntityKind.Company, _companyId, 200, ct);
                var tags = await sp.GetRequiredService<TagReadService>().GetAllAsync(false, ct);
                return (header, people, projects, tasks, followUps, comms, relations, timeline, tags);
            });

            if (data.header is null)
            {
                ErrorMessage = "La empresa ya no existe.";
                return;
            }

            Header = data.header;
            People.Reset(data.people.Where(p => p.CompanyId == _companyId));
            Projects.Reset(data.projects);
            Tasks.Reset(data.tasks);
            FollowUps.Reset(data.followUps);
            Communications.Reset(data.comms);
            Relations.Reset(data.relations);
            Timeline.Reset(data.timeline);
            AllTags.Reset(data.tags);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la empresa: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleEditPanel()
    {
        IsEditPanelOpen = !IsEditPanelOpen;
        if (IsEditPanelOpen && Header is not null)
        {
            EditName = Header.Name;
            EditKind = Header.Kind;
            EditWebsite = Header.Website ?? string.Empty;
            EditNotes = Header.Notes ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveDetailsAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            ErrorMessage = "El nombre no puede quedar vacío.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<CompanyService>().UpdateDetailsAsync(new UpdateCompanyRequest
            {
                Id = _companyId,
                Name = EditName.Trim(),
                Kind = EditKind,
                Website = EditWebsite,
                Notes = EditNotes,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        IsEditPanelOpen = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Header is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<CompanyService>().SetFavoriteAsync(_companyId, !Header.IsFavorite, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (Header is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) =>
        {
            var service = sp.GetRequiredService<CompanyService>();
            return Header.IsArchived ? service.UnarchiveAsync(_companyId, ct) : service.ArchiveAsync(_companyId, ct);
        });
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddTagAsync(TagListItem? tag)
    {
        if (tag is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<CompanyService>().AddTagAsync(_companyId, tag.Id, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RemoveTagAsync(TagChip? tag)
    {
        if (tag is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<CompanyService>().RemoveTagAsync(_companyId, tag.Id, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleComposer() => Composer.IsOpen = !Composer.IsOpen;

    [RelayCommand]
    private async Task LogCommunicationAsync()
    {
        if (string.IsNullOrWhiteSpace(Composer.Subject))
        {
            ErrorMessage = "Indica el asunto de la comunicación.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<CommunicationService>().LogAsync(new LogCommunicationRequest
            {
                Channel = Composer.Channel,
                Direction = Composer.Direction,
                Subject = Composer.Subject.Trim(),
                Body = Composer.Body,
                OccurredAtUtc = Composer.When?.UtcDateTime,
                CompanyId = _companyId,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        Composer.Reset();
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleRelationComposer() => RelationComposer.IsOpen = !RelationComposer.IsOpen;

    [RelayCommand]
    private async Task SearchRelationTargetsAsync()
    {
        RelationComposer.Results.Clear();
        var hits = await unitOfWork.RunAsync((sp, ct) => RelationComposer.SearchAsync(sp, ct));
        foreach (var hit in hits)
        {
            RelationComposer.Results.Add(hit);
        }
    }

    [RelayCommand]
    private async Task AddRelationAsync(RelationTargetHit? hit)
    {
        if (hit is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<RelationService>().AddAsync(new AddRelationRequest
            {
                FromKind = EntityKind.Company,
                FromId = _companyId,
                ToKind = hit.Kind,
                ToId = hit.Id,
                Kind = RelationComposer.Kind,
                Note = RelationComposer.Note,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        RelationComposer.Reset();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RemoveRelationAsync(RelationView? relation)
    {
        if (relation is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<RelationService>().RemoveAsync(relation.Id, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenPerson(PersonListItem? person)
    {
        if (person is not null)
        {
            navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(person.Id));
        }
    }

    [RelayCommand]
    private void OpenProject(ProjectListItem? project)
    {
        if (project is not null)
        {
            navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(project.Id));
        }
    }

    [RelayCommand]
    private void OpenTask(WorkTaskListItem? task)
    {
        if (task is not null)
        {
            navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(task.Id));
        }
    }

    [RelayCommand]
    private void OpenRelation(RelationView? relation)
    {
        if (relation is null)
        {
            return;
        }

        switch (relation.OtherKind)
        {
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(relation.OtherId));
                break;
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(relation.OtherId));
                break;
            case EntityKind.Person:
                navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(relation.OtherId));
                break;
            case EntityKind.Company:
                navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(relation.OtherId));
                break;
            default:
                if (relation.OtherProjectId is { } pid)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(pid));
                }

                break;
        }
    }

    [RelayCommand]
    private void Back() => navigation.GoBack();
}
