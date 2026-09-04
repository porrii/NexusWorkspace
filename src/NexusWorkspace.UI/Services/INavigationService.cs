using NexusWorkspace.UI.ViewModels;

namespace NexusWorkspace.UI.Services;

/// <summary>Section keys for the primary navigation rail.</summary>
public enum PageKey
{
    Dashboard,
    Inbox,
    Projects,
    Tasks,
    FollowUps,
    Calendar,
    People,
    Companies,
    Tags,
    Files,
    Activity,
    Statistics,
    Archived,
    Settings,

    // Detail pages (not in the rail)
    ProjectDetail,
    TaskDetail,
    PersonDetail,
    CompanyDetail,
}

/// <summary>
/// Resolves a view model from DI, lets the caller configure it, and asks the
/// navigation host (the shell) to show it.
/// </summary>
public interface INavigationService
{
    ViewModelBase? Current { get; }

    event EventHandler<ViewModelBase>? Navigated;

    void NavigateTo<TViewModel>(Action<TViewModel>? configure = null)
        where TViewModel : ViewModelBase;

    void NavigateTo(PageKey key);

    bool CanGoBack { get; }

    void GoBack();
}
