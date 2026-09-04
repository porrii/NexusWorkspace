using CommunityToolkit.Mvvm.ComponentModel;
using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels;

/// <summary>Shown for navigation sections that arrive in a later phase.</summary>
public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Próximamente";

    [ObservableProperty]
    private string _description = "Esta sección llegará en una fase posterior del roadmap.";

    public PageKey Key { get; private set; } = PageKey.Dashboard;

    public PlaceholderViewModel For(PageKey key)
    {
        Key = key;
        Title = key switch
        {
            PageKey.Inbox => "Inbox",
            PageKey.Tasks => "Tareas",
            PageKey.FollowUps => "Seguimientos",
            PageKey.Calendar => "Calendario",
            PageKey.People => "Personas",
            PageKey.Companies => "Empresas",
            PageKey.Tags => "Etiquetas",
            PageKey.Files => "Archivos",
            PageKey.Statistics => "Estadísticas",
            PageKey.Archived => "Archivados",
            _ => "Próximamente",
        };
        return this;
    }
}
