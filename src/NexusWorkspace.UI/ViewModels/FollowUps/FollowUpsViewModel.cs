using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.FollowUps;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.FollowUps;

public partial class FollowUpsViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private bool _showResolved;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<FollowUpListItem> Items { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnShowResolvedChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var scope = ShowResolved ? FollowUpScope.All : FollowUpScope.Open;
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<FollowUpReadService>().GetListAsync(scope, ct));

            // oldest first already; keep the biggest waits on top
            Items.Reset(rows);
            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar los seguimientos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task SendReminderAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().SendReminderAsync(item.Id, null, ct));

    [RelayCommand]
    private Task RecordContactAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().RecordContactAsync(item.Id, null, ct));

    [RelayCommand]
    private Task EscalateAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().EscalateAsync(item.Id, ct));

    [RelayCommand]
    private Task MarkAnsweredAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().ResolveAsync(
                new ResolveFollowUpRequest { Id = item.Id, State = FollowUpState.Answered }, ct));

    [RelayCommand]
    private Task CloseAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().ResolveAsync(
                new ResolveFollowUpRequest { Id = item.Id, State = FollowUpState.Closed }, ct));

    [RelayCommand]
    private Task ReopenAsync(FollowUpListItem? item)
        => item is null ? Task.CompletedTask : RunAndRefreshAsync((sp, ct) =>
            sp.GetRequiredService<FollowUpService>().ReopenAsync(item.Id, ct));

    [RelayCommand]
    private void Open(FollowUpListItem? item)
    {
        if (item is null)
        {
            return;
        }

        switch (item.TargetKind)
        {
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(item.TargetId));
                break;
            default:
                if (item.ProjectId is { } pid)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(pid));
                }

                break;
        }
    }

    private async Task RunAndRefreshAsync(Func<IServiceProvider, CancellationToken, Task<Application.Common.Result>> operation)
    {
        try
        {
            var result = await unitOfWork.RunAsync(operation);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
