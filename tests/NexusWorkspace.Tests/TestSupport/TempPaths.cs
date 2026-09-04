using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Tests.TestSupport;

/// <summary>Throwaway <see cref="IAppPaths"/> rooted in a fresh temp directory; deletes itself on dispose.</summary>
public sealed class TempPaths : IAppPaths, IDisposable
{
    public TempPaths()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), "nexus-tests", Guid.NewGuid().ToString("N"));
        EnsureCreated();
    }

    public string RootDirectory { get; }

    public bool IsPortable => true;

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
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort; the OS temp cleaner will get it.
        }
    }
}
