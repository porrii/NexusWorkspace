using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IAppPaths"/>: portable mode when a file named
/// <c>nexus.portable</c> sits next to the executable (data goes to <c>.\data\</c>),
/// otherwise <c>%APPDATA%\NexusWorkspace\</c>. The Android head can supply its own
/// implementation later.
/// </summary>
public sealed class AppPaths : IAppPaths
{
    public AppPaths(string? overrideRootDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideRootDirectory))
        {
            RootDirectory = overrideRootDirectory;
            IsPortable = false;
            return;
        }

        var executableDirectory = AppContext.BaseDirectory;
        var portableMarker = Path.Combine(executableDirectory, "nexus.portable");

        if (File.Exists(portableMarker))
        {
            IsPortable = true;
            RootDirectory = Path.Combine(executableDirectory, "data");
        }
        else
        {
            IsPortable = false;
            RootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
                "NexusWorkspace");
        }
    }

    public string RootDirectory { get; }

    public bool IsPortable { get; }

    public string DatabasePath => Path.Combine(RootDirectory, "nexus.db");

    public string FilesDirectory => Path.Combine(RootDirectory, "files");

    public string BackupsDirectory => Path.Combine(RootDirectory, "backups");

    public string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public string ExportsDirectory => Path.Combine(RootDirectory, "exports");

    public string SettingsFilePath => Path.Combine(RootDirectory, "settings.json");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(FilesDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ExportsDirectory);
    }
}
