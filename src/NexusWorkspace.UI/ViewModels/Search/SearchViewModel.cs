using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Search;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;
using NexusWorkspace.UI.ViewModels.Tasks;

namespace NexusWorkspace.UI.ViewModels.Search;

public partial class SearchViewModel(IUnitOfWorkRunner unitOfWork, INavigationService navigation) : ViewModelBase
{
    private CancellationTokenSource? _debounce;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private bool _noResults;

    public ObservableCollection<SearchHit> Results { get; } = [];

    public event EventHandler? RequestClose;

    public void Reset()
    {
        _debounce?.Cancel();
        Query = string.Empty;
        Results.Clear();
        NoResults = false;
        ErrorMessage = null;
    }

    partial void OnQueryChanged(string value) => _ = RunAsync(value);

    private async Task RunAsync(string text)
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();

        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length < 2)
        {
            Results.Clear();
            NoResults = false;
            return;
        }

        try
        {
            await Task.Delay(140, cts.Token);

            var hits = await unitOfWork.RunAsync((sp, ct) =>
                sp.GetRequiredService<ISearchService>().SearchAsync(trimmed, 40, ct));

            if (cts.IsCancellationRequested)
            {
                return;
            }

            Results.Reset(hits);
            NoResults = hits.Count == 0;
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer keystroke
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo buscar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Open(SearchHit? hit)
    {
        if (hit is null)
        {
            return;
        }

        RequestClose?.Invoke(this, EventArgs.Empty);

        switch (hit.NavigateKind)
        {
            case EntityKind.Project:
                navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(hit.NavigateId));
                break;

            case EntityKind.WorkTask:
                navigation.NavigateTo<TaskDetailViewModel>(vm => vm.Load(hit.NavigateId));
                break;

            default:
                if (hit.ProjectId is { } projectId)
                {
                    navigation.NavigateTo<ProjectDetailViewModel>(vm => vm.Load(projectId));
                }

                break;
        }
    }
}
