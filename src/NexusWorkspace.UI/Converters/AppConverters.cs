using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.Application.Common;
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

    public static readonly FuncValueConverter<DateTime, string> LocalTime =
        new(value => value.ToLocalTime().ToString("HH:mm"));

    public static readonly FuncValueConverter<DateTime?, string?> RelativeDate =
        new(value => value is null ? null : Relative(value.Value));

    public static readonly FuncValueConverter<DateTime, string> RelativeDateTime =
        new(Relative);

    public static readonly FuncValueConverter<int, string> Percent = new(value => value + "%");

    public static readonly FuncValueConverter<int, double> PercentFraction = new(value => value / 100d);

    public static readonly FuncValueConverter<bool, double> SidebarWidth = new(expanded => expanded ? 234d : 64d);

    /// <summary>Bar-chart fraction (0..1) → pixel width, with a visible minimum.</summary>
    public static readonly FuncValueConverter<double, double> BarPixels =
        new(fraction => Math.Max(3d, Math.Clamp(fraction, 0d, 1d) * 240d));

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

    public static readonly FuncValueConverter<AttachmentKind, string> AttachmentKindIcon = new(kind => kind switch
    {
        AttachmentKind.Image => "FileImageOutline",
        AttachmentKind.Pdf => "FilePdfBox",
        AttachmentKind.Document => "FileWordOutline",
        AttachmentKind.Spreadsheet => "FileExcelOutline",
        AttachmentKind.Archive => "FolderZipOutline",
        AttachmentKind.Audio => "FileMusicOutline",
        AttachmentKind.Video => "FileVideoOutline",
        AttachmentKind.Text => "FileDocumentOutline",
        _ => "FileOutline",
    });

    public static readonly FuncValueConverter<ActivityType, string> ActivityTypeIcon = new(type => type switch
    {
        ActivityType.Created => "PlusCircleOutline",
        ActivityType.StatusChanged => "SwapHorizontal",
        ActivityType.PriorityChanged => "FlagOutline",
        ActivityType.DueDateChanged => "CalendarClock",
        ActivityType.CommentAdded => "CommentTextOutline",
        ActivityType.AttachmentAdded => "PaperclipPlus",
        ActivityType.AttachmentRemoved => "PaperclipOff",
        ActivityType.FollowUpStarted or ActivityType.FollowUpReminderSent => "ClockAlertOutline",
        ActivityType.FollowUpResolved => "ClockCheckOutline",
        ActivityType.QuickAction => "FlashOutline",
        ActivityType.CommunicationLogged => "MessageOutline",
        ActivityType.MeetingScheduled or ActivityType.MeetingUpdated => "CalendarAccountOutline",
        ActivityType.RelationAdded or ActivityType.RelationRemoved => "LinkVariant",
        ActivityType.TagAdded or ActivityType.TagRemoved => "TagOutline",
        ActivityType.Archived or ActivityType.Restored => "ArchiveOutline",
        ActivityType.Trashed or ActivityType.RestoredFromTrash or ActivityType.Deleted => "DeleteOutline",
        ActivityType.Renamed => "RenameBox",
        _ => "CircleSmall",
    });

    public static readonly FuncValueConverter<ActivityType, IBrush> ActivityTypeBrush = new(type => type switch
    {
        ActivityType.Created => Brush("#2E9E63"),
        ActivityType.StatusChanged => Brush("#4A43D9"),
        ActivityType.PriorityChanged => Brush("#DD8330"),
        ActivityType.CommentAdded => Brush("#5C7CB0"),
        ActivityType.AttachmentAdded => Brush("#2F7DA3"),
        ActivityType.AttachmentRemoved => Brush("#9A2F3A"),
        ActivityType.FollowUpStarted or ActivityType.FollowUpReminderSent => Brush("#7A5AD1"),
        ActivityType.FollowUpResolved => Brush("#2E9E63"),
        ActivityType.QuickAction => Brush("#B8951F"),
        ActivityType.CommunicationLogged => Brush("#4A43D9"),
        ActivityType.MeetingScheduled or ActivityType.MeetingUpdated => Brush("#2F7DA3"),
        ActivityType.Trashed or ActivityType.Deleted => Brush("#9A2F3A"),
        _ => Brush("#6A7183"),
    });

    /// <summary>File path → a decoded, down-scaled bitmap for image thumbnails. Null on any failure.</summary>
    public static readonly FuncValueConverter<string?, Bitmap?> Thumbnail = new(path =>
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, 260);
        }
        catch (Exception)
        {
            return null;
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
        TemplateKind tk => DisplayNames.Of(tk),
        Application.Trash.TrashScope ts => DisplayNames.Of(ts),
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
