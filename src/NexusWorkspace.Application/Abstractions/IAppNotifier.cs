namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Shows a transient on-screen notification (toast). Implemented per platform head
/// (Windows: an in-app notification manager). A no-op fallback is used elsewhere.
/// Native OS toasts are a later refinement.
/// </summary>
public interface IAppNotifier
{
    void Toast(string title, string? body);
}

/// <summary>Fallback that does nothing. Registered by the UI layer.</summary>
public sealed class NullAppNotifier : IAppNotifier
{
    public void Toast(string title, string? body)
    {
    }
}
