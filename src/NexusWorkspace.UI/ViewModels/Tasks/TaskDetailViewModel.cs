using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Domain.Tasks;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Shared;

namespace NexusWorkspace.UI.ViewModels.Tasks;

public partial class TaskDetailViewModel(
    IUnitOfWorkRunner unitOfWork,
    INavigationService navigation,
    IPlatformLauncher launcher,
    ISettingsStore settings) : ViewModelBase
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
    private string _newCommentBody = string.Empty;

    [ObservableProperty]
    private string _newEventLabel = string.Empty;

    [ObservableProperty]
    private string _eventNote = string.Empty;

    [ObservableProperty]
    private bool _isReminderPanelOpen;

    [ObservableProperty]
    private string _newReminderText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _newReminderDate = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty]
    private TimeSpan? _newReminderTime = new(9, 0, 0);

    [ObservableProperty]
    private RecurrenceFrequency _newReminderRecurrence = RecurrenceFrequency.None;

    [ObservableProperty]
    private int _newReminderRecurrenceInterval = 1;

    [ObservableProperty]
    private DateTimeOffset? _newReminderRecurrenceEnd;

    [ObservableProperty]
    private bool _isEditPanelOpen;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private Priority _editPriority = Priority.Medium;

    [ObservableProperty]
    private DateTimeOffset? _editDueDate;

    [ObservableProperty]
    private TimeSpan? _editDueTime;

    [ObservableProperty]
    private PersonListItem? _editAssignee;

    [ObservableProperty]
    private CompanyListItem? _editCompany;

    [ObservableProperty]
    private bool _isTemplatePanelOpen;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string? _templateMessage;

    public ObservableCollection<ReminderView> Reminders { get; } = [];

    public IReadOnlyList<RecurrenceFrequency> RecurrenceOptions { get; } = Enum.GetValues<RecurrenceFrequency>();

    /// <summary>Subtasks and legacy checklist items merged into one editable list.</summary>
    public ObservableCollection<TaskChildRow> Children { get; } = [];

    public ObservableCollection<TaskCommentView> Comments { get; } = [];

    public ObservableCollection<ActivityEntry> History { get; } = [];

    public ObservableCollection<WorkTaskStatus> AvailableStatuses { get; } = [];

    public ObservableCollection<PersonListItem> AllPeople { get; } = [];

    public ObservableCollection<PersonListItem> CollaboratorCandidates { get; } = [];

    public ObservableCollection<CompanyListItem> AllCompanies { get; } = [];

    public ObservableCollection<TagListItem> AllTags { get; } = [];

    public AttachmentsSectionViewModel Attachments { get; } = new(unitOfWork, launcher);

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    /// <summary>One-tap "task event" labels; grows as the user registers new ones.</summary>
    public ObservableCollection<string> EventLabels { get; } = [];

    public CommunicationComposerViewModel CommComposer { get; } = new();

    public int ChildProgress => Children.Count == 0
        ? 0
        : (int)Math.Round(100.0 * Children.Count(c => c.IsDone) / Children.Count);

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
                var reminderReads = sp.GetRequiredService<ReminderReadService>();
                await sp.GetRequiredService<WorkTaskService>().EnsureChecklistConvertedAsync(_taskId, ct);
                var d = await taskReads.GetDetailAsync(_taskId, ct);
                var h = await activityReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, 200, ct);
                var r = await reminderReads.GetForEntityAsync(EntityKind.WorkTask, _taskId, ct);
                var people = await sp.GetRequiredService<PersonReadService>().GetListAsync(PersonScope.All, null, null, ct);
                var companies = await sp.GetRequiredService<CompanyReadService>().GetListAsync(CompanyScope.All, null, null, ct);
                var tags = await sp.GetRequiredService<TagReadService>().GetAllAsync(false, ct);
                return (d, h, r, people, companies, tags);
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

            Children.Reset(TaskChildRow.BuildFrom(detail));
            Comments.Reset(detail.Comments);
            History.Reset(data.h);
            Reminders.Reset(data.r);
            AllPeople.Reset(data.people);
            AllCompanies.Reset(data.companies);
            AllTags.Reset(data.tags);
            EventLabels.Reset(settings.Current.TaskEventLabels);
            var linkedPeople = detail.Collaborators.Select(p => p.Id)
                .Append(detail.AssigneePersonId ?? Guid.Empty).ToHashSet();
            CollaboratorCandidates.Reset(data.people.Where(p => !linkedPeople.Contains(p.Id)).OrderBy(p => p.Name));

            Attachments.Bind(EntityKind.WorkTask, _taskId, detail.ProjectId, RefreshAsync);
            await Attachments.LoadAsync();

            OnPropertyChanged(nameof(ChildProgress));
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
    private void ToggleEditPanel()
    {
        IsEditPanelOpen = !IsEditPanelOpen;
        if (!IsEditPanelOpen || Detail is null)
        {
            return;
        }

        EditTitle = Detail.Title;
        EditDescription = Detail.Description ?? string.Empty;
        EditPriority = Detail.Priority;

        if (Detail.DueDateUtc is { } due)
        {
            var local = due.ToLocalTime();
            EditDueDate = new DateTimeOffset(DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified), DateTimeOffset.Now.Offset);
            EditDueTime = local.TimeOfDay;
        }
        else
        {
            EditDueDate = null;
            EditDueTime = null;
        }

        EditAssignee = Detail.AssigneePersonId is { } aid
            ? AllPeople.FirstOrDefault(p => p.Id == aid)
            : null;
        EditCompany = Detail.RelatedCompanyId is { } cid
            ? AllCompanies.FirstOrDefault(c => c.Id == cid)
            : null;
    }

    [RelayCommand]
    private async Task SaveDetailsAsync()
    {
        if (Detail is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            ErrorMessage = "El título no puede quedar vacío.";
            return;
        }

        DateTime? dueUtc = null;
        var clearDue = EditDueDate is null;
        if (EditDueDate is { } d)
        {
            var local = d.Date + (EditDueTime ?? TimeSpan.Zero);
            dueUtc = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        }

        var request = new UpdateWorkTaskDetailsRequest
        {
            Id = _taskId,
            Title = EditTitle.Trim(),
            Description = EditDescription ?? string.Empty,
            Priority = EditPriority,
            ClearDueDate = clearDue,
            DueDateUtc = dueUtc,
            ChangeAssignee = true,
            AssigneePersonId = EditAssignee?.Id,
            ChangeRelatedCompany = true,
            RelatedCompanyId = EditCompany?.Id,
        };

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().UpdateDetailsAsync(request, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        IsEditPanelOpen = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private Task AddTagAsync(TagListItem? tag)
        => tag is null || Detail is null || Detail.Tags.Any(x => x.Id == tag.Id)
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().AddTagAsync(_taskId, tag.Id, ct));

    [RelayCommand]
    private Task RemoveTagAsync(TagChip? tag)
        => tag is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().RemoveTagAsync(_taskId, tag.Id, ct));

    [RelayCommand]
    private void ClearEditDueDate()
    {
        EditDueDate = null;
        EditDueTime = null;
    }

    [RelayCommand]
    private Task AddCollaboratorAsync(PersonListItem? person)
        => person is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().LinkPersonAsync(_taskId, person.Id, ct));

    [RelayCommand]
    private Task RemoveCollaboratorAsync(PersonListItem? person)
        => person is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
                sp.GetRequiredService<WorkTaskService>().UnlinkPersonAsync(_taskId, person.Id, ct));

    [RelayCommand]
    private void ClearEditAssignee() => EditAssignee = null;

    [RelayCommand]
    private void ClearEditCompany() => EditCompany = null;

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
    private Task ToggleChildAsync(TaskChildRow? row)
        => row is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
            {
                var svc = sp.GetRequiredService<WorkTaskService>();
                return row.IsChecklist
                    ? svc.SetChecklistItemCheckedAsync(row.Id, !row.IsDone, ct)
                    : svc.SetSubTaskDoneAsync(row.Id, !row.IsDone, ct);
            });

    [RelayCommand]
    private void StartRenameChild(TaskChildRow? row)
    {
        if (row is null)
        {
            return;
        }

        row.EditText = row.Title;
        row.IsEditing = true;
    }

    [RelayCommand]
    private void CancelRenameChild(TaskChildRow? row)
    {
        if (row is not null)
        {
            row.IsEditing = false;
        }
    }

    [RelayCommand]
    private async Task CommitRenameChildAsync(TaskChildRow? row)
    {
        if (row is null)
        {
            return;
        }

        var text = row.EditText?.Trim();
        row.IsEditing = false;
        if (string.IsNullOrWhiteSpace(text) || text == row.Title)
        {
            return;
        }

        await RunAndRefreshAsync((sp, ct) =>
        {
            var svc = sp.GetRequiredService<WorkTaskService>();
            return row.IsChecklist
                ? svc.RenameChecklistItemAsync(row.Id, text, ct)
                : svc.RenameSubTaskAsync(row.Id, text, ct);
        });
    }

    [RelayCommand]
    private Task DeleteChildAsync(TaskChildRow? row)
        => row is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
            {
                var svc = sp.GetRequiredService<WorkTaskService>();
                return row.IsChecklist
                    ? svc.DeleteChecklistItemAsync(row.Id, ct)
                    : svc.DeleteSubTaskAsync(row.Id, ct);
            });

    [RelayCommand]
    private Task MoveChildUpAsync(TaskChildRow? row) => MoveChildAsync(row, up: true);

    [RelayCommand]
    private Task MoveChildDownAsync(TaskChildRow? row) => MoveChildAsync(row, up: false);

    private Task MoveChildAsync(TaskChildRow? row, bool up)
        => row is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) =>
            {
                var svc = sp.GetRequiredService<WorkTaskService>();
                return row.IsChecklist
                    ? svc.MoveChecklistItemAsync(row.Id, up, ct)
                    : svc.MoveSubTaskAsync(row.Id, up, ct);
            });

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
    private async Task LogEventAsync(string? label)
    {
        var raw = (string.IsNullOrWhiteSpace(label) ? NewEventLabel : label)?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string text = raw;
        var note = EventNote;
        var isNew = !EventLabels.Contains(text, StringComparer.OrdinalIgnoreCase);
        NewEventLabel = string.Empty;
        EventNote = string.Empty;

        await RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<WorkTaskService>().LogEventAsync(_taskId, text, note, ct));

        if (isNew)
        {
            await settings.UpdateAsync(s =>
            {
                if (!s.TaskEventLabels.Contains(text, StringComparer.OrdinalIgnoreCase))
                {
                    s.TaskEventLabels.Add(text);
                }
            });
            EventLabels.Add(text);
        }
    }

    [RelayCommand]
    private void ToggleCommComposer() => CommComposer.IsOpen = !CommComposer.IsOpen;

    [RelayCommand]
    private async Task LogCommunicationAsync()
    {
        if (string.IsNullOrWhiteSpace(CommComposer.Subject))
        {
            ErrorMessage = "Indica el asunto de la comunicación.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<CommunicationService>().LogAsync(new LogCommunicationRequest
            {
                Channel = CommComposer.Channel,
                Direction = CommComposer.Direction,
                Subject = CommComposer.Subject.Trim(),
                Body = CommComposer.Body,
                OccurredAtUtc = CommComposer.When?.UtcDateTime,
                WorkTaskId = _taskId,
                ProjectId = Detail?.ProjectId,
                PersonId = Detail?.AssigneePersonId,
                CompanyId = Detail?.RelatedCompanyId,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        CommComposer.Reset();
        await RefreshAsync();
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
    private void ToggleReminderPanel()
    {
        IsReminderPanelOpen = !IsReminderPanelOpen;
        if (!IsReminderPanelOpen)
        {
            NewReminderText = string.Empty;
            NewReminderRecurrence = RecurrenceFrequency.None;
            NewReminderRecurrenceInterval = 1;
            NewReminderRecurrenceEnd = null;
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

        var localWhen = NewReminderDate.Value.Date + (NewReminderTime ?? new TimeSpan(9, 0, 0));

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ReminderService>().CreateAsync(new CreateReminderRequest
            {
                Text = text,
                RemindAtUtc = DateTime.SpecifyKind(localWhen, DateTimeKind.Local).ToUniversalTime(),
                TargetKind = EntityKind.WorkTask,
                TargetId = _taskId,
                RecurrenceFrequency = NewReminderRecurrence,
                RecurrenceInterval = NewReminderRecurrenceInterval,
                RecurrenceEndUtc = NewReminderRecurrenceEnd?.UtcDateTime,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        NewReminderText = string.Empty;
        NewReminderRecurrence = RecurrenceFrequency.None;
        NewReminderRecurrenceInterval = 1;
        NewReminderRecurrenceEnd = null;
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
