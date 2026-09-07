using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels;
using NexusWorkspace.UI.ViewModels.Activity;
using NexusWorkspace.UI.ViewModels.Calendar;
using NexusWorkspace.UI.ViewModels.CommandPalette;
using NexusWorkspace.UI.ViewModels.Companies;
using NexusWorkspace.UI.ViewModels.Dashboard;
using NexusWorkspace.UI.ViewModels.Files;
using NexusWorkspace.UI.ViewModels.FollowUps;
using NexusWorkspace.UI.ViewModels.Inbox;
using NexusWorkspace.UI.ViewModels.People;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.QuickCapture;
using NexusWorkspace.UI.ViewModels.Search;
using NexusWorkspace.UI.ViewModels.Settings;
using NexusWorkspace.UI.ViewModels.Shell;
using NexusWorkspace.UI.ViewModels.Statistics;
using NexusWorkspace.UI.ViewModels.Tags;
using NexusWorkspace.UI.ViewModels.Tasks;
using NexusWorkspace.UI.ViewModels.Templates;

namespace NexusWorkspace.UI;

public static class DependencyInjection
{
    /// <summary>
    /// Registers shared UI services and view models. Platform heads register their
    /// own <see cref="IPlatformLauncher"/>, hotkey and scheduler services after this.
    /// </summary>
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IQuickCaptureLauncher, QuickCaptureLauncher>();
        services.TryAddSingleton<IPlatformLauncher, NullPlatformLauncher>();

        services.AddSingleton<InAppNotifier>();
        services.AddSingleton<IAppNotifier>(sp => sp.GetRequiredService<InAppNotifier>());

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<CommandPaletteViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<NotificationCenterViewModel>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<InboxViewModel>();
        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<ProjectDetailViewModel>();
        services.AddTransient<TaskDetailViewModel>();
        services.AddTransient<FollowUpsViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<PeopleViewModel>();
        services.AddTransient<PersonDetailViewModel>();
        services.AddTransient<CompaniesViewModel>();
        services.AddTransient<CompanyDetailViewModel>();
        services.AddTransient<TagsViewModel>();
        services.AddTransient<FilesViewModel>();
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<TemplatesViewModel>();
        services.AddTransient<ActivityViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PlaceholderViewModel>();
        services.AddTransient<QuickCaptureViewModel>();

        return services;
    }
}
