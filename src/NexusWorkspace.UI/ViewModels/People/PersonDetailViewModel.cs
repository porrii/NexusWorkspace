using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.Meetings;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Companies;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Shared;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.People;

public partial class PersonDetailViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private Guid _personId;

    [ObservableProperty]
    private PersonDetail? _header;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isEditPanelOpen;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editRole = string.Empty;

    [ObservableProperty]
    private string _editEmail = string.Empty;

    [ObservableProperty]
    private string _editPhone = string.Empty;

    [ObservableProperty]
    private string _editNotes = string.Empty;

    [ObservableProperty]
    private CompanyListItem? _editCompany;

    public CommunicationComposerViewModel Composer { get; } = new();

    public MeetingComposerViewModel MeetingComposer { get; } = new();

    public RelationComposerViewModel RelationComposer { get; } = new();

    public ObservableCollection<ProjectListItem> Projects { get; } = [];

    public ObservableCollection<ProjectListItem> UnlinkedProjects { get; } = [];

    public ObservableCollection<CompanyListItem> AllCompanies { get; } = [];

    public ObservableCollection<WorkTaskListItem> Tasks { get; } = [];

    public ObservableCollection<CommunicationListItem> Communications { get; } = [];

    public ObservableCollection<MeetingListItem> Meetings { get; } = [];

    public ObservableCollection<RelationView> Relations { get; } = [];

    public ObservableCollection<ActivityEntry> Timeline { get; } = [];

    public ObservableCollection<TagListItem> AllTags { get; } = [];

    public void Load(Guid personId)
    {
        _personId = personId;
        _ = RefreshAsync();
    }

    public override Task OnActivatedAsync()
        => _personId == Guid.Empty ? Task.CompletedTask : RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_personId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var data = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var header = await sp.GetRequiredService<PersonReadService>().GetDetailAsync(_personId, ct);
                var projects = await sp.GetRequiredService<ProjectReadService>().GetForPersonAsync(_personId, ct);
                var allProjects = await sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.All, null, ct);
                var allCompanies = await sp.GetRequiredService<CompanyReadService>().GetListAsync(CompanyScope.All, null, null, ct);
                var tasks = await sp.GetRequiredService<WorkTaskReadService>().GetForPersonAsync(_personId, false, ct);
                var comms = await sp.GetRequiredService<CommunicationReadService>().GetForPersonAsync(_personId, 200, ct);
                var meetings = await sp.GetRequiredService<MeetingReadService>().GetForPersonAsync(_personId, ct);
                var relations = await sp.GetRequiredService<RelationReadService>().GetForEntityAsync(EntityKind.Person, _personId, ct);
                var timeline = await sp.GetRequiredService<ActivityReadService>().GetForEntityAsync(EntityKind.Person, _personId, 200, ct);
                var tags = await sp.GetRequiredService<TagReadService>().GetAllAsync(false, ct);
                return (header, projects, allProjects, allCompanies, tasks, comms, meetings, relations, timeline, tags);
            });

            if (data.header is null)
            {
                ErrorMessage = "La persona ya no existe.";
                return;
            }

            Header = data.header;
            Projects.Reset(data.projects);
            var linkedIds = data.projects.Select(p => p.Id).ToHashSet();
            UnlinkedProjects.Reset(data.allProjects.Where(p => !linkedIds.Contains(p.Id)).OrderBy(p => p.Name));
            AllCompanies.Reset(data.allCompanies);
            Tasks.Reset(data.tasks);
            Communications.Reset(data.comms);
            Meetings.Reset(data.meetings);
            Relations.Reset(data.relations);
            Timeline.Reset(data.timeline);
            AllTags.Reset(data.tags);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la persona: {ex.Message}";
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
            EditRole = Header.Role ?? string.Empty;
            EditEmail = Header.Email ?? string.Empty;
            EditPhone = Header.Phone ?? string.Empty;
            EditNotes = Header.Notes ?? string.Empty;
            EditCompany = Header.CompanyId is { } cid ? AllCompanies.FirstOrDefault(c => c.Id == cid) : null;
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
            sp.GetRequiredService<PersonService>().UpdateDetailsAsync(new UpdatePersonRequest
            {
                Id = _personId,
                Name = EditName.Trim(),
                Role = EditRole,
                Email = EditEmail,
                Phone = EditPhone,
                Notes = EditNotes,
                CompanyId = EditCompany?.Id ?? Guid.Empty,
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
    private void ClearEditCompany() => EditCompany = null;

    [RelayCommand]
    private async Task LinkProjectAsync(ProjectListItem? project)
    {
        if (project is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<ProjectService>().LinkPersonAsync(project.Id, _personId, null, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UnlinkProjectAsync(ProjectListItem? project)
    {
        if (project is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<ProjectService>().UnlinkPersonAsync(project.Id, _personId, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Header is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<PersonService>().SetFavoriteAsync(_personId, !Header.IsFavorite, ct));
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
            var service = sp.GetRequiredService<PersonService>();
            return Header.IsArchived ? service.UnarchiveAsync(_personId, ct) : service.ArchiveAsync(_personId, ct);
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

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<PersonService>().AddTagAsync(_personId, tag.Id, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RemoveTagAsync(TagChip? tag)
    {
        if (tag is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<PersonService>().RemoveTagAsync(_personId, tag.Id, ct));
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
                PersonId = _personId,
                CompanyId = Header?.CompanyId,
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
    private void ToggleMeetingComposer() => MeetingComposer.IsOpen = !MeetingComposer.IsOpen;

    [RelayCommand]
    private async Task ScheduleMeetingAsync()
    {
        if (string.IsNullOrWhiteSpace(MeetingComposer.Title) || MeetingComposer.Date is null)
        {
            ErrorMessage = "La reunión necesita título y fecha.";
            return;
        }

        var start = MeetingComposer.StartUtc();
        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<MeetingService>().ScheduleAsync(new ScheduleMeetingRequest
            {
                Title = MeetingComposer.Title.Trim(),
                Agenda = MeetingComposer.Agenda,
                StartUtc = start,
                EndUtc = MeetingComposer.DurationMinutes > 0 ? start.AddMinutes(MeetingComposer.DurationMinutes) : null,
                Location = MeetingComposer.Location,
                Participants = [new MeetingParticipantInput { PersonId = _personId }],
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        MeetingComposer.Reset();
        await RefreshAsync();
    }

    [RelayCommand]
    private void ToggleRelationComposer() => RelationComposer.IsOpen = !RelationComposer.IsOpen;

    [RelayCommand]
    private async Task SearchRelationTargetsAsync()
    {
        RelationComposer.Results.Clear();
        var hits = await unitOfWork.RunAsync((sp, ct) =>
            RelationComposer.SearchAsync(sp, ct));
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
                FromKind = EntityKind.Person,
                FromId = _personId,
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
    private void OpenCompany()
    {
        if (Header?.CompanyId is { } companyId)
        {
            navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(companyId));
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
