using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.QuickCapture;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.ViewModels.QuickCapture;

public enum QuickCaptureKind
{
    Inbox = 0,
    Task = 1,
    Project = 2,
}

/// <summary>
/// The floating capture window. Type text, press Enter — it saves. Optional
/// project/type/priority/date. The local parser only <em>suggests</em>: "Aplicar"
/// fills the fields, nothing is applied silently.
/// </summary>
public partial class QuickCaptureViewModel(IUnitOfWorkRunner unitOfWork, IQuickCaptureParser parser) : ViewModelBase
{
    private QuickCaptureParseResult? _lastParse;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private QuickCaptureKind _kind = QuickCaptureKind.Inbox;

    [ObservableProperty]
    private ProjectNameRef? _selectedProject;

    [ObservableProperty]
    private Priority _priority = Priority.Medium;

    [ObservableProperty]
    private DateTimeOffset? _dueDate;

    [ObservableProperty]
    private string _suggestion = string.Empty;

    [ObservableProperty]
    private bool _optionsExpanded;

    [ObservableProperty]
    private bool _hasSuggestion;

    public ObservableCollection<ProjectNameRef> Projects { get; } = [];

    public IReadOnlyList<QuickCaptureKind> Kinds { get; } = Enum.GetValues<QuickCaptureKind>();

    public IReadOnlyList<Priority> Priorities { get; } = Enum.GetValues<Priority>();

    public event EventHandler? RequestClose;

    public async Task PrepareAsync()
    {
        Text = string.Empty;
        Kind = QuickCaptureKind.Inbox;
        SelectedProject = null;
        Priority = Priority.Medium;
        DueDate = null;
        Suggestion = string.Empty;
        HasSuggestion = false;
        OptionsExpanded = false;
        ErrorMessage = null;
        _lastParse = null;

        var projects = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.Active, null, ct));

        Projects.Reset(projects.Select(p => new ProjectNameRef(p.Id, p.Name)));
    }

    partial void OnTextChanged(string value)
    {
        _lastParse = parser.Parse(value ?? string.Empty, Projects);
        Suggestion = BuildSuggestion(_lastParse);
        HasSuggestion = _lastParse.HasAnySuggestion;
    }

    [RelayCommand]
    private void ApplySuggestion()
    {
        if (_lastParse is not { } parse)
        {
            return;
        }

        if (parse.ProjectId is { } projectId)
        {
            SelectedProject = Projects.FirstOrDefault(p => p.Id == projectId);
            if (SelectedProject is not null)
            {
                Kind = QuickCaptureKind.Task;
            }
        }

        if (parse.Priority is { } priority)
        {
            Priority = priority;
        }

        if (parse.DueDateUtc is { } due)
        {
            DueDate = new DateTimeOffset(DateTime.SpecifyKind(due, DateTimeKind.Utc)).ToLocalTime();
        }

        OptionsExpanded = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var raw = (Text ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var title = _lastParse?.CleanText is { Length: > 0 } clean ? clean : raw;
            var dueUtc = DueDate?.UtcDateTime;

            switch (Kind)
            {
                case QuickCaptureKind.Task when SelectedProject is not null:
                    await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<WorkTaskService>().CreateAsync(
                        new CreateWorkTaskRequest
                        {
                            ProjectId = SelectedProject.Id,
                            Title = title,
                            Priority = Priority,
                            DueDateUtc = dueUtc,
                        }, ct));
                    break;

                case QuickCaptureKind.Project:
                    await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<ProjectService>().CreateAsync(
                        new CreateProjectRequest { Name = title, Priority = Priority }, ct));
                    break;

                default:
                    var hint = _lastParse is { HasAnySuggestion: true } p ? BuildSuggestion(p) : null;
                    await unitOfWork.RunAsync((sp, ct) =>
                        sp.GetRequiredService<InboxService>().CaptureAsync(raw, hint, ct));
                    break;
            }

            Text = string.Empty;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo guardar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    private static string BuildSuggestion(QuickCaptureParseResult parse)
    {
        var parts = new List<string>();
        if (parse.Action is { } action)
        {
            parts.Add($"Acción: {action}");
        }

        if (parse.ProjectName is { } project)
        {
            parts.Add($"Proyecto: {project}");
        }

        if (parse.Priority is { } priority)
        {
            parts.Add($"Prioridad: {DisplayNames.Of(priority)}");
        }

        if (parse.DueDateUtc is { } due)
        {
            parts.Add($"Fecha: {due:dd/MM/yyyy}");
        }

        if (parse.Tags.Count > 0)
        {
            parts.Add("Etiquetas: " + string.Join(", ", parse.Tags));
        }

        if (parse.People.Count > 0)
        {
            parts.Add("Personas: " + string.Join(", ", parse.People));
        }

        var builder = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("  ·  ");
            }

            builder.Append(parts[i]);
        }

        return builder.ToString();
    }
}
