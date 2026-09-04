using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Notifications;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Shell;

/// <summary>The shell "bell": unread badge + a flyout list of recent notifications.</summary>
public partial class NotificationCenterViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private bool _hasUnread;

    public ObservableCollection<NotificationView> Items { get; } = [];

    public async Task RefreshCountAsync()
    {
        try
        {
            UnreadCount = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<NotificationReadService>().CountUnreadAsync(ct));
            HasUnread = UnreadCount > 0;
        }
        catch
        {
            // badge is best-effort
        }
    }

    /// <summary>Bound to the bell button: reload the list each time it is pressed.</summary>
    [RelayCommand]
    private Task Toggle() => LoadAsync();

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<NotificationService>().MarkAllReadAsync(ct));
        await LoadAsync();
        await RefreshCountAsync();
    }

    [RelayCommand]
    private async Task OpenAsync(NotificationView? notification)
    {
        if (notification is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<NotificationService>().MarkReadAsync(notification.Id, ct));

        await RefreshCountAsync();

        switch (notification.TargetKind)
        {
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(notification.TargetId!.Value));
                break;
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(notification.TargetId!.Value));
                break;
            default:
                if (notification.ProjectId is { } pid)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(pid));
                }

                break;
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<NotificationReadService>().GetRecentAsync(40, false, ct));
            Items.Reset(rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
