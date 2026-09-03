using CommunityToolkit.Mvvm.ComponentModel;

namespace NexusWorkspace.UI.ViewModels;

/// <summary>Base class for all view models.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Called by the navigation host after the view model becomes the active page.</summary>
    public virtual Task OnActivatedAsync() => Task.CompletedTask;
}
