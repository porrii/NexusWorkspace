using Avalonia.Controls;
using Avalonia.Controls.Templates;
using NexusWorkspace.UI.ViewModels;

namespace NexusWorkspace.UI;

/// <summary>
/// Maps a view model to its view by convention:
/// <c>...ViewModels.Area.FooViewModel</c> → <c>...Views.Area.FooView</c>.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "(sin contenido)" };
        }

        var viewModelName = data.GetType().FullName!;
        var viewName = viewModelName.Replace("ViewModel", "View", StringComparison.Ordinal);
        var viewType = Type.GetType(viewName);

        if (viewType is null)
        {
            return new TextBlock { Text = $"Vista no encontrada: {viewName}" };
        }

        return (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
