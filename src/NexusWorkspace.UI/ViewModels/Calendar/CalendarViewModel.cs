using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Calendar;

public partial class CalendarViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private DateOnly _month = DateOnly.FromDateTime(DateTime.Now).AddDays(1 - DateOnly.FromDateTime(DateTime.Now).Day);

    [ObservableProperty]
    private string _monthLabel = string.Empty;

    [ObservableProperty]
    private CalendarDay? _selectedDay;

    [ObservableProperty]
    private bool _isQuickAddOpen;

    [ObservableProperty]
    private string _quickAddText = string.Empty;

    [ObservableProperty]
    private TimeSpan? _quickAddTime = new(9, 0, 0);

    [ObservableProperty]
    private RecurrenceFrequency _quickAddRecurrence = RecurrenceFrequency.None;

    [ObservableProperty]
    private int _quickAddRecurrenceInterval = 1;

    [ObservableProperty]
    private DateTimeOffset? _quickAddRecurrenceEnd;

    public ObservableCollection<CalendarWeek> Weeks { get; } = [];

    public ObservableCollection<string> WeekdayHeaders { get; } =
        ["lun", "mar", "mié", "jue", "vie", "sáb", "dom"];

    public ObservableCollection<CalendarEntry> SelectedDayEntries { get; } = [];

    public IReadOnlyList<RecurrenceFrequency> RecurrenceOptions { get; } = Enum.GetValues<RecurrenceFrequency>();

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private void PrevMonth()
    {
        _month = _month.AddMonths(-1);
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _month = _month.AddMonths(1);
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void GoToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        _month = today.AddDays(1 - today.Day);
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void SelectDay(CalendarDay? day)
    {
        SelectedDay = day;
        SelectedDayEntries.Reset(day?.Entries ?? []);
        IsQuickAddOpen = false;
        QuickAddText = string.Empty;
        QuickAddRecurrence = RecurrenceFrequency.None;
        QuickAddRecurrenceInterval = 1;
        QuickAddRecurrenceEnd = null;
    }

    [RelayCommand]
    private void ToggleQuickAdd() => IsQuickAddOpen = !IsQuickAddOpen;

    [RelayCommand]
    private async Task AddReminderAsync()
    {
        var text = QuickAddText?.Trim();
        if (string.IsNullOrWhiteSpace(text) || SelectedDay is null)
        {
            return;
        }

        var localWhen = SelectedDay.Date.ToDateTime(TimeOnly.MinValue) + (QuickAddTime ?? new TimeSpan(9, 0, 0));

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<ReminderService>().CreateAsync(new CreateReminderRequest
            {
                Text = text,
                RemindAtUtc = DateTime.SpecifyKind(localWhen, DateTimeKind.Local).ToUniversalTime(),
                RecurrenceFrequency = QuickAddRecurrence,
                RecurrenceInterval = QuickAddRecurrenceInterval,
                RecurrenceEndUtc = QuickAddRecurrenceEnd?.UtcDateTime,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        QuickAddText = string.Empty;
        QuickAddRecurrence = RecurrenceFrequency.None;
        QuickAddRecurrenceInterval = 1;
        QuickAddRecurrenceEnd = null;
        IsQuickAddOpen = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenEntry(CalendarEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        switch (entry.NavigateKind)
        {
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(entry.NavigateId));
                break;
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(entry.NavigateId));
                break;
            default:
                if (entry.ProjectId is { } pid)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(pid));
                }

                break;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var gridStart = FirstMonday(_month);
            var gridEnd = gridStart.AddDays(41); // 6 weeks

            var fromUtc = gridStart.ToDateTime(TimeOnly.MinValue).AddDays(-1).ToUniversalTime();
            var toUtc = gridEnd.ToDateTime(TimeOnly.MaxValue).AddDays(1).ToUniversalTime();

            var entries = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<CalendarReadService>().GetRangeAsync(fromUtc, toUtc, ct));

            var byDay = entries.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => (IReadOnlyList<CalendarEntry>)g.ToList());
            var today = DateOnly.FromDateTime(DateTime.Now);

            var weeks = new List<CalendarWeek>(6);
            for (var w = 0; w < 6; w++)
            {
                var days = new List<CalendarDay>(7);
                for (var d = 0; d < 7; d++)
                {
                    var date = gridStart.AddDays(w * 7 + d);
                    days.Add(new CalendarDay
                    {
                        Date = date,
                        InCurrentMonth = date.Month == _month.Month && date.Year == _month.Year,
                        IsToday = date == today,
                        Entries = byDay.TryGetValue(date, out var list) ? list : [],
                    });
                }

                weeks.Add(new CalendarWeek { Days = days });
            }

            Weeks.Reset(weeks);
            MonthLabel = Capitalise(_month.ToString("MMMM yyyy", new CultureInfo("es-ES")));

            var keep = SelectedDay?.Date;
            var newSelection = weeks.SelectMany(x => x.Days).FirstOrDefault(x => x.Date == (keep ?? today))
                               ?? weeks.SelectMany(x => x.Days).FirstOrDefault(x => x.Date == today);
            SelectDay(newSelection);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar el calendario: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static DateOnly FirstMonday(DateOnly monthStart)
    {
        var offset = ((int)monthStart.DayOfWeek + 6) % 7; // Mon=0 … Sun=6
        return monthStart.AddDays(-offset);
    }

    private static string Capitalise(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0], CultureInfo.CurrentCulture) + value[1..];
}
