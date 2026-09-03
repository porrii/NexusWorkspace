namespace NexusWorkspace.UI.Services;

/// <summary>Opens the floating quick-capture window. Driven by the global hotkey and the command palette.</summary>
public interface IQuickCaptureLauncher
{
    /// <summary>Show if hidden, hide if already visible.</summary>
    void Toggle();

    /// <summary>Always bring it up, ready for input.</summary>
    void Show();
}
