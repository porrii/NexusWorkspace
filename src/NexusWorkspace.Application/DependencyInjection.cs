using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.QuickCapture;
using NexusWorkspace.Application.Tasks;

namespace NexusWorkspace.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application services. Requires an <see cref="IApplicationDbContext"/>,
    /// <see cref="IClock"/>, <see cref="IAppPaths"/> and <see cref="ISettingsStore"/>
    /// to be registered by the infrastructure layer.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWorkRunner, ScopedUnitOfWorkRunner>();

        services.AddScoped<IActivityLog, ActivityLog>();
        services.AddSingleton<IQuickCaptureParser, RuleBasedQuickCaptureParser>();

        services.AddScoped<ProjectService>();
        services.AddScoped<WorkTaskService>();
        services.AddScoped<InboxService>();

        services.AddScoped<ProjectReadService>();
        services.AddScoped<WorkTaskReadService>();
        services.AddScoped<ActivityReadService>();
        services.AddScoped<InboxReadService>();

        services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
        services.AddScoped<IValidator<CreateWorkTaskRequest>, CreateWorkTaskRequestValidator>();

        return services;
    }
}
