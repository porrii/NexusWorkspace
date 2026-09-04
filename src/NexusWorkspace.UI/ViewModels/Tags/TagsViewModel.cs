using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Tags;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.Tags;

public partial class TagsViewModel(IUnitOfWorkRunner unitOfWork) : ViewModelBase
{
    [ObservableProperty]
    private bool _isCreatePanelOpen;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string _newColor = string.Empty;

    [ObservableProperty]
    private string _newDescription = string.Empty;

    [ObservableProperty]
    private TagListItem? _mergeSource;

    [ObservableProperty]
    private TagListItem? _mergeTarget;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<TagListItem> Tags { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var rows = await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<TagReadService>().GetAllAsync(true, ct));
            Tags.Reset(rows);
            IsEmpty = Tags.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar las etiquetas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleCreatePanel()
    {
        IsCreatePanelOpen = !IsCreatePanelOpen;
        if (!IsCreatePanelOpen)
        {
            NewName = string.Empty;
            NewColor = string.Empty;
            NewDescription = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateTagAsync()
    {
        var name = NewName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Escribe un nombre.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TagService>().CreateAsync(new CreateTagRequest
            {
                Name = name,
                Color = NewColor,
                Description = NewDescription,
            }, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        IsCreatePanelOpen = false;
        NewName = string.Empty;
        NewColor = string.Empty;
        NewDescription = string.Empty;
        await RefreshAsync();
    }

    [RelayCommand]
    private Task TogglePinAsync(TagListItem? tag)
        => tag is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) => sp.GetRequiredService<TagService>().UpdateAsync(
                new UpdateTagRequest { Id = tag.Id, IsPinned = !tag.IsPinned }, ct));

    [RelayCommand]
    private Task DeleteAsync(TagListItem? tag)
        => tag is null
            ? Task.CompletedTask
            : RunAndRefreshAsync((sp, ct) => sp.GetRequiredService<TagService>().DeleteAsync(tag.Id, ct));

    [RelayCommand]
    private async Task MergeAsync()
    {
        if (MergeSource is null || MergeTarget is null || MergeSource.Id == MergeTarget.Id)
        {
            ErrorMessage = "Elige dos etiquetas distintas para fusionar.";
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TagService>().MergeAsync(MergeSource.Id, MergeTarget.Id, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        MergeSource = null;
        MergeTarget = null;
        await RefreshAsync();
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
