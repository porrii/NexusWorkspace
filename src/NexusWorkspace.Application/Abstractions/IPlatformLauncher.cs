namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Opens folders and links in the OS. Implemented per platform head; a no-op
/// fallback is used when no head provides one.
/// </summary>
public interface IPlatformLauncher
{
    void OpenFolder(string path);

    void OpenUrl(string url);

    /// <summary>Opens a file with the OS default application for its type.</summary>
    void OpenPath(string path);

    /// <summary>Opens the containing folder with the file selected, when the OS supports it.</summary>
    void RevealInFolder(string path);
}

/// <summary>Fallback that does nothing. Registered by the UI layer.</summary>
public sealed class NullPlatformLauncher : IPlatformLauncher
{
    public void OpenFolder(string path)
    {
    }

    public void OpenUrl(string url)
    {
    }

    public void OpenPath(string path)
    {
    }

    public void RevealInFolder(string path)
    {
    }
}
