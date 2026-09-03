namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Resolves every on-disk location the app uses. Implemented per platform (Windows
/// AppData, portable folder next to the exe, Android app storage) so the rest of
/// the code never touches <c>Environment.SpecialFolder</c> directly.
/// </summary>
public interface IAppPaths
{
    /// <summary>Root workspace directory that contains everything below.</summary>
    string RootDirectory { get; }

    /// <summary>Full path of the SQLite database file (<c>nexus.db</c>).</summary>
    string DatabasePath { get; }

    /// <summary>Directory for attachment files and their thumbnails.</summary>
    string FilesDirectory { get; }

    /// <summary>Directory where rotated database backups are written.</summary>
    string BackupsDirectory { get; }

    /// <summary>Directory for Serilog rolling log files.</summary>
    string LogsDirectory { get; }

    /// <summary>Directory for exported workspace packages.</summary>
    string ExportsDirectory { get; }

    /// <summary>Full path of the user/app settings file (<c>settings.json</c>).</summary>
    string SettingsFilePath { get; }

    /// <summary>True when running in portable mode (data lives next to the executable).</summary>
    bool IsPortable { get; }

    /// <summary>Creates any missing directories. Safe to call repeatedly.</summary>
    void EnsureCreated();
}
