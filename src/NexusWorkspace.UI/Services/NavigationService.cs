using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.UI.ViewModels;
using NexusWorkspace.UI.ViewModels.Activity;
using NexusWorkspace.UI.ViewModels.Calendar;
using NexusWorkspace.UI.ViewModels.Companies;
using NexusWorkspace.UI.ViewModels.Dashboard;
using NexusWorkspace.UI.ViewModels.Files;
using NexusWorkspace.UI.ViewModels.FollowUps;
using NexusWorkspace.UI.ViewModels.Inbox;
using NexusWorkspace.UI.ViewModels.People;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Settings;
using NexusWorkspace.UI.ViewModels.Tasks;
using NexusWorkspace.UI.ViewModels.Statistics;
using NexusWorkspace.UI.ViewModels.Tags;
using NexusWorkspace.UI.ViewModels.Templates;
using NexusWorkspace.UI.ViewModels.Trash;

namespace NexusWorkspace.UI.Services;

/// <inheritdoc cref="INavigationService" />
public sealed class NavigationService(IServiceProvider services) : INavigationService
{
    private readonly Stack<ViewModelBase> _backStack = new();

    public ViewModelBase? Current { get; private set; }

    public event EventHandler<ViewModelBase>? Navigated;

    public bool CanGoBack => _backStack.Count > 0;

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null)
        where TViewModel : ViewModelBase
    {
        var viewModel = services.GetRequiredService<TViewModel>();
        configure?.Invoke(viewModel);
        Show(viewModel);
    }

    public void NavigateTo(PageKey key)
    {
        ViewModelBase viewModel = key switch
        {
            PageKey.Dashboard => services.GetRequiredService<DashboardViewModel>(),
            PageKey.Inbox => services.GetRequiredService<InboxViewModel>(),
            PageKey.Projects => services.GetRequiredService<ProjectsViewModel>(),
            PageKey.Tasks => services.GetRequiredService<TasksViewModel>(),
            PageKey.FollowUps => services.GetRequiredService<FollowUpsViewModel>(),
            PageKey.Calendar => services.GetRequiredService<CalendarViewModel>(),
            PageKey.People => services.GetRequiredService<PeopleViewModel>(),
            PageKey.Companies => services.GetRequiredService<CompaniesViewModel>(),
            PageKey.Tags => services.GetRequiredService<TagsViewModel>(),
            PageKey.Files => services.GetRequiredService<FilesViewModel>(),
            PageKey.Statistics => services.GetRequiredService<StatisticsViewModel>(),
            PageKey.Templates => services.GetRequiredService<TemplatesViewModel>(),
            PageKey.Archived => services.GetRequiredService<TrashViewModel>(),
            PageKey.Activity => services.GetRequiredService<ActivityViewModel>(),
            PageKey.Settings => services.GetRequiredService<SettingsViewModel>(),
            _ => services.GetRequiredService<PlaceholderViewModel>().For(key),
        };

        Show(viewModel);
    }

    public void GoBack()
    {
        if (_backStack.Count == 0)
        {
            return;
        }

        var previous = _backStack.Pop();
        Current = previous;
        Navigated?.Invoke(this, previous);
        _ = previous.OnActivatedAsync();
    }

    private void Show(ViewModelBase viewModel)
    {
        if (Current is not null && !ReferenceEquals(Current, viewModel))
        {
            _backStack.Push(Current);
        }

        Current = viewModel;
        Navigated?.Invoke(this, viewModel);
        _ = viewModel.OnActivatedAsync();
    }
}
