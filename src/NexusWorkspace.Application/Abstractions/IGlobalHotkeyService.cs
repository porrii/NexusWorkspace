namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Registers the OS-wide quick-capture hotkey (Ctrl+Shift+Space on Windows).
/// Implemented per platform head; absent on platforms without global hotkeys.
/// </summary>
public interface IGlobalHotkeyService
{
    void Start();

    void Stop();
}
