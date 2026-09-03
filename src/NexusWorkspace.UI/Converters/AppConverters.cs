using Avalonia.Data.Converters;
using Avalonia.Media;
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

    private static string? EnumToLabel(object? value) => value switch
    {
        ProjectStatus s => DisplayNames.Of(s),
        WorkTaskStatus s => DisplayNames.Of(s),
        Priority p => DisplayNames.Of(p),
        CompanyKind k => DisplayNames.Of(k),
        QuickActionKind q => DisplayNames.Of(q),
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
