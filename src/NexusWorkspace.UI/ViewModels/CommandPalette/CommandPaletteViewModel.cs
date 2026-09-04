using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Settings;
using NexusWorkspace.UI.Services;
using NexusWorkspace.UI.ViewModels.Projects;

namespace NexusWorkspace.UI.ViewModels.CommandPalette;

/// <param name="Title">What the user reads.</param>
/// <param name="Category">Grouping label ("Navegación", "Crear", "Apariencia").</param>
/// <param name="Icon">Material icon kind.</param>
/// <param name="Execute">Runs after the palette closes.</param>
public sealed record PaletteCommand(string Title, string Category, string Icon, Func<Task> Execute);

public partial class CommandPaletteViewModel : ViewModelBase
{
    private readonly IReadOnlyList<PaletteCommand> _all;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private PaletteCommand? _selected;

    public CommandPaletteViewModel(
        INavigationService navigation,
        IThemeService theme,
        IQuickCaptureLauncher quickCapture)
    {
        _all =
        [
            new("Ir al Dashboard", "Navegación", "ViewDashboard", Nav(navigation, PageKey.Dashboard)),
            new("Ir a Inbox", "Navegación", "Inbox", Nav(navigation, PageKey.Inbox)),
            new("Ir a Proyectos", "Navegación", "FolderMultiple", Nav(navigation, PageKey.Projects)),
            new("Ir a Seguimientos", "Navegación", "ClockAlert", Nav(navigation, PageKey.FollowUps)),
            new("Ir al Calendario", "Navegación", "Calendar", Nav(navigation, PageKey.Calendar)),
            new("Ir a Actividad", "Navegación", "History", Nav(navigation, PageKey.Activity)),
            new("Ir a Configuración", "Navegación", "Cog", Nav(navigation, PageKey.Settings)),
            new("Nuevo proyecto", "Crear", "Plus", () =>
            {
                navigation.NavigateTo<ProjectsViewModel>(vm => vm.IsCreatePanelOpen = true);
                return Task.CompletedTask;
            }),
            new("Captura rápida", "Crear", "FlashOutline", () =>
            {
                quickCapture.Show();
                return Task.CompletedTask;
            }),
            new("Tema: claro", "Apariencia", "WhiteBalanceSunny", () => theme.SetAndPersistAsync(ThemeMode.Light)),
            new("Tema: oscuro", "Apariencia", "WeatherNight", () => theme.SetAndPersistAsync(ThemeMode.Dark)),
            new("Tema: según el sistema", "Apariencia", "ThemeLightDark", () => theme.SetAndPersistAsync(ThemeMode.System)),
        ];

        Filter();
    }

    public ObservableCollection<PaletteCommand> Results { get; } = [];

    public event EventHandler? RequestClose;

    public void Reset()
    {
        Query = string.Empty;
        Filter();
    }

    partial void OnQueryChanged(string value) => Filter();

    [RelayCommand]
    private async Task RunAsync(PaletteCommand? command)
    {
        command ??= Selected;
        if (command is null)
        {
            return;
        }

        RequestClose?.Invoke(this, EventArgs.Empty);
        try
        {
            await command.Execute();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void Filter()
    {
        var matches = string.IsNullOrWhiteSpace(Query)
            ? _all
            : _all.Where(c => TextNormalization.FuzzyMatch(c.Title, Query)
                              || TextNormalization.FuzzyMatch(c.Category, Query));

        Results.Reset(matches.Take(40));
        Selected = Results.FirstOrDefault();
    }

    private static Func<Task> Nav(INavigationService navigation, PageKey key) => () =>
    {
        navigation.NavigateTo(key);
        return Task.CompletedTask;
    };
}
