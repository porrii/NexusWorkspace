using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Calendar;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Communications;
using NexusWorkspace.Application.Companies;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Meetings;
using NexusWorkspace.Application.Notifications;
using NexusWorkspace.Application.People;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.QuickCapture;
using NexusWorkspace.Application.Relations;
using NexusWorkspace.Application.Reminders;
using NexusWorkspace.Application.SavedSearches;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.Application.Tasks;

namespace NexusWorkspace.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application services. Requires an <see cref="IApplicationDbContext"/>,
    /// <see cref="IClock"/>, <see cref="IAppPaths"/>, <see cref="ISettingsStore"/> and
    /// <see cref="IAppNotifier"/> to be registered by the infrastructure / head layers.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWorkRunner, ScopedUnitOfWorkRunner>();

        services.AddScoped<IActivityLog, ActivityLog>();
        services.AddSingleton<IQuickCaptureParser, RuleBasedQuickCaptureParser>();

        services.AddScoped<ProjectService>();
        services.AddScoped<WorkTaskService>();
        services.AddScoped<InboxService>();
        services.AddScoped<FollowUpService>();
        services.AddScoped<ReminderService>();
        services.AddScoped<PersonService>();
        services.AddScoped<CompanyService>();
        services.AddScoped<CommunicationService>();
        services.AddScoped<MeetingService>();
        services.AddScoped<TagService>();
        services.AddScoped<RelationService>();
        services.AddScoped<SavedSearchService>();

        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());

        services.AddScoped<ProjectReadService>();
        services.AddScoped<WorkTaskReadService>();
        services.AddScoped<ActivityReadService>();
        services.AddScoped<InboxReadService>();
        services.AddScoped<FollowUpReadService>();
        services.AddScoped<ReminderReadService>();
        services.AddScoped<NotificationReadService>();
        services.AddScoped<CalendarReadService>();
        services.AddScoped<PersonReadService>();
        services.AddScoped<CompanyReadService>();
        services.AddScoped<CommunicationReadService>();
        services.AddScoped<MeetingReadService>();
        services.AddScoped<TagReadService>();
        services.AddScoped<RelationReadService>();
        services.AddScoped<SavedSearchReadService>();

        services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
        services.AddScoped<IValidator<CreateWorkTaskRequest>, CreateWorkTaskRequestValidator>();

        return services;
    }
}
