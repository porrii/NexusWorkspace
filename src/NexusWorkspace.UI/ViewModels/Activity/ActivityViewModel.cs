using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Activity;

namespace NexusWorkspace.UI.ViewModels.Activity;

public partial class ActivityViewModel(IUnitOfWorkRunner unitOfWork) : ViewModelBase
{
    [ObservableProperty]
    private int _limit = 100;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<ActivityEntry> Entries { get; } = [];

    public IReadOnlyList<int> LimitOptions { get; } = [50, 100, 250, 500];

    public override Task OnActivatedAsync() => RefreshAsync();

    partial void OnLimitChanged(int value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var rows = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ActivityReadService>().GetRecentAsync(Limit, ct));

            Entries.Reset(rows);
            IsEmpty = Entries.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar la actividad: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
