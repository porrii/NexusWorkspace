using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Trash;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Companies;
using NexusWorkspace.UI.ViewModels.People;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Trash;

public partial class TrashViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private TrashScope _scope = TrashScope.Both;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private TrashEntry? _pendingPurge;

    [ObservableProperty]
    private bool _confirmingEmpty;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<TrashEntry> Entries { get; } = [];

    public IReadOnlyList<TrashScope> Scopes { get; } = Enum.GetValues<TrashScope>();

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnScopeChanged(TrashScope value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        PendingPurge = null;
        ConfirmingEmpty = false;

        try
        {
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<TrashReadService>().GetAsync(Scope, ct));
            Entries.Reset(rows);
            IsEmpty = Entries.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la papelera: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(TrashEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<TrashService>().RestoreAsync(entry.Kind, entry.Id, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        StatusMessage = $"«{entry.Name}» restaurado.";
        await RefreshAsync();
    }

    [RelayCommand]
    private void AskPurge(TrashEntry? entry)
    {
        ConfirmingEmpty = false;
        PendingPurge = entry;
    }

    [RelayCommand]
    private void CancelPurge()
    {
        PendingPurge = null;
        ConfirmingEmpty = false;
    }

    [RelayCommand]
    private async Task ConfirmPurgeAsync()
    {
        if (PendingPurge is not { } entry)
        {
            return;
        }

        PendingPurge = null;

        var result = await unitOfWork.RunAsync(async (sp, ct) =>
        {
            await sp.GetRequiredService<IBackupService>().CreateAsync("prepurge", false, ct);
            return await sp.GetRequiredService<TrashService>().PurgeAsync(entry.Kind, entry.Id, ct);
        });

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        StatusMessage = $"«{entry.Name}» eliminado permanentemente (se guardó una copia antes).";
        await RefreshAsync();
    }

    [RelayCommand]
    private void AskEmpty()
    {
        PendingPurge = null;
        ConfirmingEmpty = true;
    }

    [RelayCommand]
    private async Task ConfirmEmptyAsync()
    {
        ConfirmingEmpty = false;

        var removed = await unitOfWork.RunAsync(async (sp, ct) =>
        {
            await sp.GetRequiredService<IBackupService>().CreateAsync("prepurge", false, ct);
            return await sp.GetRequiredService<TrashService>().EmptyTrashAsync(ct);
        });

        StatusMessage = $"Papelera vaciada: {removed} elemento(s) eliminados (se guardó una copia antes).";
        await RefreshAsync();
    }

    [RelayCommand]
    private void Open(TrashEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        switch (entry.Kind)
        {
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(entry.Id));
                break;
            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(entry.Id));
                break;
            case EntityKind.Person:
                navigation.NavigateTo<PersonDetailViewModel>(vm => vm.Load(entry.Id));
                break;
            case EntityKind.Company:
                navigation.NavigateTo<CompanyDetailViewModel>(vm => vm.Load(entry.Id));
                break;
        }
    }
}
