using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.ViewModels.Activity;

/// <summary>A dropdown option that carries a nullable value plus a display label.</summary>
public sealed record FilterOption<T>(string Label, T? Value)
    where T : struct;

/// <summary>Project filter option (reference type, so its own record).</summary>
public sealed record ProjectOption(string Label, Guid? Id);

/// <summary>Activity entries that happened on one day.</summary>
public sealed class ActivityDayGroup
{
    public required string Header { get; init; }

    public required IReadOnlyList<ActivityEntry> Entries { get; init; }
}

public partial class ActivityViewModel(IUnitOfWorkRunner unitOfWork) : ViewModelBase
{
    private bool _loaded;
    private bool _suppressRefresh;

    [ObservableProperty]
    private int _limit = 250;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private ProjectOption? _selectedProject;

    [ObservableProperty]
    private FilterOption<EntityKind>? _selectedKind;

    [ObservableProperty]
    private DateTimeOffset? _fromDate;

    [ObservableProperty]
    private DateTimeOffset? _toDate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<ActivityDayGroup> Days { get; } = [];

    public ObservableCollection<ProjectOption> ProjectOptions { get; } = [];

    public IReadOnlyList<FilterOption<EntityKind>> KindOptions { get; } =
    [
        new("Todo", null),
        .. new[]
        {
            EntityKind.Project, EntityKind.WorkTask, EntityKind.Person, EntityKind.Company,
            EntityKind.FollowUp, EntityKind.Communication, EntityKind.Meeting, EntityKind.Reminder,
        }.Select(k => new FilterOption<EntityKind>(DisplayNames.Of(k), k)),
    ];

    public IReadOnlyList<int> LimitOptions { get; } = [100, 250, 500, 1000];

    public override async Task OnActivatedAsync()
    {
        if (!_loaded)
        {
            await LoadProjectsAsync();
            _loaded = true;
        }

        await RefreshAsync();
    }

    partial void OnLimitChanged(int value) => QueueRefresh();

    partial void OnSelectedProjectChanged(ProjectOption? value) => QueueRefresh();

    partial void OnSelectedKindChanged(FilterOption<EntityKind>? value) => QueueRefresh();

    partial void OnFromDateChanged(DateTimeOffset? value) => QueueRefresh();

    partial void OnToDateChanged(DateTimeOffset? value) => QueueRefresh();

    private void QueueRefresh()
    {
        if (!_suppressRefresh)
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _suppressRefresh = true;
        SelectedProject = ProjectOptions.Count > 0 ? ProjectOptions[0] : null;
        SelectedKind = KindOptions[0];
        FromDate = null;
        ToDate = null;
        SearchText = string.Empty;
        _suppressRefresh = false;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private Task ApplySearchAsync() => RefreshAsync();

    private async Task LoadProjectsAsync()
    {
        var projects = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ProjectReadService>().GetListAsync(ProjectListScope.All, null, ct));

        _suppressRefresh = true;
        ProjectOptions.Reset(
            new[] { new ProjectOption("Todos los proyectos", null) }
                .Concat(projects.Select(p => new ProjectOption(p.Name, p.Id))));
        SelectedProject = ProjectOptions[0];
        SelectedKind = KindOptions[0];
        _suppressRefresh = false;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            static DateTime? ToUtc(DateTimeOffset? d) => d is { } v
                ? DateTime.SpecifyKind(v.Date, DateTimeKind.Local).ToUniversalTime()
                : null;

            var filter = new ActivityFilter
            {
                ProjectId = SelectedProject?.Id,
                TargetKind = SelectedKind?.Value,
                FromUtc = ToUtc(FromDate),
                ToUtc = ToDate is { } t
                    ? DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime()
                    : null,
                Text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            };

            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ActivityReadService>().GetFilteredAsync(filter, Limit, ct));

            var groups = rows
                .GroupBy(r => r.OccurredAtUtc.ToLocalTime().Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new ActivityDayGroup
                {
                    Header = DayHeader(g.Key),
                    Entries = g.OrderByDescending(r => r.OccurredAtUtc).ToList(),
                });

            Days.Reset(groups);
            IsEmpty = Days.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la actividad: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string DayHeader(DateTime day)
    {
        var today = DateTime.Now.Date;
        if (day == today)
        {
            return "Hoy";
        }

        if (day == today.AddDays(-1))
        {
            return "Ayer";
        }

        return day.ToString("dddd d 'de' MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
    }
}
