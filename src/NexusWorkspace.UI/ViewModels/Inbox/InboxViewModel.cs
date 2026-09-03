using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Inbox;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.QuickCapture;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Inbox;

public partial class InboxViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private ProjectNameRef? _targetProject;

    [ObservableProperty]
    private string _newCaptureText = string.Empty;

    public ObservableCollection<InboxItemView> Items { get; } = [];

    public ObservableCollection<ProjectNameRef> Projects { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var (items, projects) = await unitOfWork.RunAsync(async (sp, ct) =>
            {
                var inboxReads = sp.GetRequiredService<InboxReadService>();
                var projectReads = sp.GetRequiredService<ProjectReadService>();
                var pending = await inboxReads.GetPendingAsync(ct);
                var active = await projectReads.GetListAsync(ProjectListScope.Active, null, ct);
                return (pending, active);
            });

            Items.Reset(items);
            Projects.Reset(projects.Select(p => new ProjectNameRef(p.Id, p.Name)));
            TargetProject ??= Projects.FirstOrDefault();
            IsEmpty = Items.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar el Inbox: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CaptureAsync()
    {
        var text = NewCaptureText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        NewCaptureText = string.Empty;
        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<InboxService>().CaptureAsync(text, null, ct));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ConvertToTaskAsync(InboxItemView? item)
    {
        if (item is null)
        {
            return;
        }

        if (TargetProject is null)
        {
            ErrorMessage = "Elige un proyecto de destino primero.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<InboxService>().ConvertToTaskAsync(item.Id, TargetProject.Id, new ConvertToTaskOptions
            {
                Title = item.RawText,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        var taskId = result.Value;
        await RefreshAsync();
        navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(taskId));
    }

    [RelayCommand]
    private async Task ConvertToProjectAsync(InboxItemView? item)
    {
        if (item is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<InboxService>().ConvertToProjectAsync(item.Id, new ConvertToProjectOptions
            {
                Name = item.RawText,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        var projectId = result.Value;
        await RefreshAsync();
        navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(projectId));
    }

    [RelayCommand]
    private async Task DismissAsync(InboxItemView? item)
    {
        if (item is null)
        {
            return;
        }

        await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<InboxService>().DismissAsync(item.Id, ct));
        await RefreshAsync();
    }
}
