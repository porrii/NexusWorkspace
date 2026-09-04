using Avalonia.Controls;
using Avalonia.Input;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.UI.ViewModels.Calendar;

namespace NexusWorkspace.UI.Views.Calendar;

public partial class CalendarView : UserControl
{
    public CalendarView() => InitializeComponent();

    private void OnDayClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: CalendarDay day } && DataContext is CalendarViewModel vm)
        {
            vm.SelectDayCommand.Execute(day);
        }
    }

    private void OnEntryClicked(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control { Tag: CalendarEntry entry } && DataContext is CalendarViewModel vm)
        {
            vm.OpenEntryCommand.Execute(entry);
        }
    }
}
