using Avalonia.Data.Converters;
using Avalonia.Media;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.Application.Localization;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.Converters;

/// <summary>Reusable <see cref="FuncValueConverter{TIn,TOut}"/> instances for XAML bindings.</summary>
public static class AppConverters
{
    public static readonly FuncValueConverter<object?, string?> EnumLabel = new(EnumToLabel);

    public static readonly FuncValueConverter<DateTime?, string?> LocalDate =
        new(value => value?.ToLocalTime().ToString("dd/MM/yyyy"));

    public static readonly FuncValueConverter<DateTime, string> LocalDateTime =
        new(value => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));

    public static readonly FuncValueConverter<DateTime?, string?> RelativeDate =
        new(value => value is null ? null : Relative(value.Value));

    public static readonly FuncValueConverter<DateTime, string> RelativeDateTime =
        new(Relative);

    public static readonly FuncValueConverter<int, string> Percent = new(value => value + "%");

    public static readonly FuncValueConverter<int, double> PercentFraction = new(value => value / 100d);

    public static readonly FuncValueConverter<bool, double> SidebarWidth = new(expanded => expanded ? 234d : 64d);

    public static readonly FuncValueConverter<bool, double> ExpandedToOpacity = new(expanded => expanded ? 1d : 0d);

    public static readonly FuncValueConverter<string?, bool> HasText =
        new(value => !string.IsNullOrWhiteSpace(value));

    public static readonly FuncValueConverter<string?, bool> IsBlank =
        new(value => string.IsNullOrWhiteSpace(value));

    public static readonly FuncValueConverter<object?, bool> IsNotNull = new(value => value is not null);

    public static readonly FuncValueConverter<int, bool> Positive = new(value => value > 0);

    public static readonly FuncValueConverter<int, bool> IsZero = new(value => value == 0);

    public static readonly FuncValueConverter<ReminderStatus, bool> ReminderPending =
        new(status => status == ReminderStatus.Pending);

    public static readonly FuncValueConverter<Priority, IBrush> PriorityBrush = new(priority => priority switch
    {
        Priority.Critical => Brush("#D93A44"),
        Priority.High => Brush("#DD8330"),
        Priority.Medium => Brush("#B8951F"),
        _ => Brush("#5C7CB0"),
    });

    public static readonly FuncValueConverter<WorkTaskStatus, IBrush> TaskStatusBrush = new(status => status switch
    {
        WorkTaskStatus.InProgress => Brush("#4A43D9"),
        WorkTaskStatus.WaitingClient or WorkTaskStatus.WaitingProvider => Brush("#7A5AD1"),
        WorkTaskStatus.Blocked => Brush("#9A2F3A"),
        WorkTaskStatus.Finished => Brush("#2E9E63"),
        WorkTaskStatus.Cancelled => Brush("#6A7183"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<ProjectStatus, IBrush> ProjectStatusBrush = new(status => status switch
    {
        ProjectStatus.Active => Brush("#4A43D9"),
        ProjectStatus.OnHold => Brush("#B8951F"),
        ProjectStatus.Blocked => Brush("#9A2F3A"),
        ProjectStatus.Finished => Brush("#2E9E63"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<FollowUpState, IBrush> FollowUpStateBrush = new(state => state switch
    {
        FollowUpState.Escalated => Brush("#D93A44"),
        FollowUpState.Waiting => Brush("#7A5AD1"),
        FollowUpState.Answered => Brush("#2E9E63"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<CalendarEntryKind, IBrush> CalendarKindBrush = new(kind => kind switch
    {
        CalendarEntryKind.TaskDue => Brush("#4A43D9"),
        CalendarEntryKind.ProjectDue => Brush("#9A2F3A"),
        CalendarEntryKind.FollowUpNext => Brush("#7A5AD1"),
        CalendarEntryKind.Reminder => Brush("#DD8330"),
        CalendarEntryKind.Meeting => Brush("#2F7DA3"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<CompanyKind, IBrush> CompanyKindBrush = new(kind => kind switch
    {
        CompanyKind.Client => Brush("#2E9E63"),
        CompanyKind.Provider => Brush("#DD8330"),
        CompanyKind.Internal => Brush("#4A43D9"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<CommunicationDirection, IBrush> CommunicationDirectionBrush = new(direction => direction switch
    {
        CommunicationDirection.Outbound => Brush("#4A43D9"),
        CommunicationDirection.Inbound => Brush("#2E9E63"),
        _ => Brush("#6A7183"),
    });

    public static readonly FuncValueConverter<CommunicationChannel, string> CommunicationChannelIcon = new(channel => channel switch
    {
        CommunicationChannel.Email => "EmailOutline",
        CommunicationChannel.Call => "PhoneOutline",
        CommunicationChannel.Chat => "ChatOutline",
        CommunicationChannel.InPerson => "AccountVoice",
        CommunicationChannel.Letter => "MailboxOutline",
        CommunicationChannel.Ticket => "TicketOutline",
        _ => "MessageOutline",
    });

    public static readonly FuncValueConverter<MeetingStatus, IBrush> MeetingStatusBrush = new(status => status switch
    {
        MeetingStatus.Scheduled => Brush("#4A43D9"),
        MeetingStatus.Held => Brush("#2E9E63"),
        MeetingStatus.Cancelled => Brush("#6A7183"),
        _ => Brush("#6A7183"),
    });

    /// <summary>Hex string → brush; falls back to a neutral grey for anything unparseable.</summary>
    public static readonly FuncValueConverter<string?, IBrush> HexBrush = new(hex =>
    {
        try
        {
            return string.IsNullOrWhiteSpace(hex) ? Brush("#6A7183") : new SolidColorBrush(Color.Parse(hex));
        }
        catch (FormatException)
        {
            return Brush("#6A7183");
        }
    });

    /// <summary>Name → up to two uppercase initials, e.g. "Iván Béznilla" → "IB".</summary>
    public static readonly FuncValueConverter<string?, string> Initials = new(name =>
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0][..1];
        var second = parts.Length > 1 ? parts[^1][..1] : string.Empty;
        return (first + second).ToUpperInvariant();
    });

    /// <summary>UTC datetime → "hoy" / "hace 1 d" / "en 3 d".</summary>
    public static readonly FuncValueConverter<DateTime, string> DayDelta = new(utc =>
    {
        var days = (int)Math.Round((DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date - DateTime.Now.Date).TotalDays);
        return days switch
        {
            0 => "hoy",
            1 => "mañana",
            -1 => "ayer",
            > 1 => $"en {days} d",
            _ => $"hace {-days} d",
        };
    });

    private static string? EnumToLabel(object? value) => value switch
    {
        ProjectStatus s => DisplayNames.Of(s),
        WorkTaskStatus s => DisplayNames.Of(s),
        Priority p => DisplayNames.Of(p),
        CompanyKind k => DisplayNames.Of(k),
        QuickActionKind q => DisplayNames.Of(q),
        CommunicationChannel c => DisplayNames.Of(c),
        CommunicationDirection d => DisplayNames.Of(d),
        MeetingStatus m => DisplayNames.Of(m),
        RelationKind r => DisplayNames.Of(r),
        FollowUpState f => DisplayNames.Of(f),
        ReminderStatus rs => DisplayNames.Of(rs),
        EntityKind e => DisplayNames.Of(e),
        null => null,
        _ => value.ToString(),
    };

    private static string Relative(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var now = DateTime.Now;
        var delta = now - local;

        if (delta.TotalSeconds < 60)
        {
            return "ahora mismo";
        }

        if (delta.TotalMinutes < 60)
        {
            return "hace " + (int)delta.TotalMinutes + " min";
        }

        if (local.Date == now.Date)
        {
            return "hoy " + local.ToString("HH:mm");
        }

        if (local.Date == now.Date.AddDays(-1))
        {
            return "ayer " + local.ToString("HH:mm");
        }

        if (delta.TotalDays < 7)
        {
            return "hace " + (int)delta.TotalDays + " d";
        }

        return local.ToString("dd/MM/yyyy");
    }

    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));
}
