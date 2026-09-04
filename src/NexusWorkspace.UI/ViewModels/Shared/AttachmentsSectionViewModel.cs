using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Attachments;
using NexusWorkspace.Domain.Enums;

namespace NexusWorkspace.UI.ViewModels.Shared;

/// <summary>
/// Reusable "Adjuntos" panel embedded by the project and task detail screens.
/// Owns the file list plus add (picker / drag-drop / paste), remove and open.
/// </summary>
public partial class AttachmentsSectionViewModel(IUnitOfWorkRunner unitOfWork, IPlatformLauncher launcher) : ObservableObject
{
    private EntityKind _kind;
    private Guid _id;
    private Guid? _projectId;
    private Func<Task>? _onChanged;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isDragActive;

    public ObservableCollection<AttachmentListItem> Items { get; } = [];

    public bool IsEmpty => Items.Count == 0;

    public void Bind(EntityKind kind, Guid id, Guid? projectId, Func<Task>? onChanged = null)
    {
        _kind = kind;
        _id = id;
        _projectId = projectId;
        _onChanged = onChanged;
    }

    public async Task LoadAsync()
    {
        if (_id == Guid.Empty)
        {
            return;
        }

        try
        {
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<AttachmentReadService>().GetForEntityAsync(_kind, _id, ct));
            Items.Reset(rows);
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron cargar los adjuntos: {ex.Message}";
        }
    }

    public async Task AddStorageFilesAsync(IReadOnlyList<IStorageFile>? files)
    {
        if (files is null || files.Count == 0 || _id == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync();
                var result = await unitOfWork.RunAsync((sp, ct) =>
                    sp.GetRequiredService<AttachmentService>().AddAsync(_kind, _id, file.Name, stream, _projectId, ct));

                if (result.IsFailure)
                {
                    ErrorMessage = result.Error.Message;
                }
            }

            await LoadAsync();
            if (_onChanged is not null)
            {
                await _onChanged();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo adjuntar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Open(AttachmentListItem? item)
    {
        if (item is not null)
        {
            launcher.OpenPath(item.AbsolutePath);
        }
    }

    [RelayCommand]
    private void Reveal(AttachmentListItem? item)
    {
        if (item is not null)
        {
            launcher.RevealInFolder(item.AbsolutePath);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(AttachmentListItem? item)
    {
        if (item is null)
        {
            return;
        }

        var result = await unitOfWork.RunAsync((sp, ct) =>
            sp.GetRequiredService<AttachmentService>().RemoveAsync(item.Id, ct));

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await LoadAsync();
        if (_onChanged is not null)
        {
            await _onChanged();
        }
    }
}
