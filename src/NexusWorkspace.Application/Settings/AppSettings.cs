namespace NexusWorkspace.Application.Settings;

/// <summary>App appearance mode. "System" follows the OS setting.</summary>
public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>
/// User and application preferences. Serialised to <c>settings.json</c> — never mixed
/// with business data. Add new properties with sensible defaults; old files stay valid.
/// </summary>
public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public ThemeMode Theme { get; set; } = ThemeMode.System;

    public bool SidebarExpanded { get; set; } = true;

    public string StartupSection { get; set; } = "Dashboard";

    public WindowPlacement Window { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public BackupSettings Backup { get; set; } = new();

    /// <summary>Visibility of each dashboard widget, keyed by widget id.</summary>
    public Dictionary<string, bool> DashboardWidgets { get; set; } = new();

    /// <summary>Ordering of dashboard widgets, by widget id.</summary>
    public List<string> DashboardWidgetOrder { get; set; } = [];

    public List<string> RecentProjectIds { get; set; } = [];
}

public sealed class WindowPlacement
{
    public double Width { get; set; } = 1280;

    public double Height { get; set; } = 820;

    public double? X { get; set; }

    public double? Y { get; set; }

    public bool Maximized { get; set; }
}

public sealed class NotificationSettings
{
    public bool Enabled { get; set; } = true;

    public bool RemindersOnStartup { get; set; } = true;

    public bool FollowUpNudges { get; set; } = true;
}

public sealed class BackupSettings
{
    /// <summary>None, Daily or Weekly.</summary>
    public string Schedule { get; set; } = "Daily";

    public int KeepVersions { get; set; } = 30;

    public bool BackupBeforeMigration { get; set; } = true;
}
