using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Export;
using NexusWorkspace.Application.Statistics;

namespace NexusWorkspace.UI.ViewModels.Statistics;

/// <summary>A labelled value plus its share of the largest value in the group (0..1).</summary>
public sealed record BarRow(string Label, string Value, double Fraction);

public partial class StatisticsViewModel(IUnitOfWorkRunner unitOfWork, IPlatformLauncher launcher) : ViewModelBase
{
    [ObservableProperty]
    private WorkspaceStats? _stats;

    [ObservableProperty]
    private string _attachmentSize = "0 B";

    [ObservableProperty]
    private string? _exportMessage;

    public ObservableCollection<BarRow> ProjectsByStatus { get; } = [];

    public ObservableCollection<BarRow> TasksByStatus { get; } = [];

    public ObservableCollection<BarRow> TasksByPriority { get; } = [];

    public ObservableCollection<BarRow> FinishedPerWeek { get; } = [];

    public ObservableCollection<BarRow> TopPeople { get; } = [];

    public ObservableCollection<BarRow> ActivityTypes { get; } = [];

    public override Task OnActivatedAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var stats = await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<StatsReadService>().GetAsync(ct));
            Stats = stats;
            AttachmentSize = FileKinds.HumanSize(stats.AttachmentBytes);

            Fill(ProjectsByStatus, stats.ProjectsByStatus.Select(x => (x.Name, x.Count)));
            Fill(TasksByStatus, stats.TasksByStatus.Select(x => (x.Name, x.Count)));
            Fill(TasksByPriority, stats.TasksByPriority.Select(x => (x.Name, x.Count)));
            Fill(FinishedPerWeek, stats.FinishedPerWeek.Select(x => (x.WeekStart.ToString("dd/MM"), x.Count)));
            Fill(TopPeople, stats.TopPeopleByOpenTasks.Select(x => (x.Name, x.Count)));
            Fill(ActivityTypes, stats.ActivityByTypeLast30.Select(x => (x.Name, x.Count)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron calcular las estadísticas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSummaryAsync(string format)
    {
        if (Stats is null)
        {
            return;
        }

        var kind = string.Equals(format, "json", StringComparison.OrdinalIgnoreCase) ? ExportFormat.Json : ExportFormat.Csv;

        var rows = new List<IReadOnlyList<string?>>
        {
            new string?[] { "Proyectos totales", Stats.TotalProjectCount.ToString() },
            new string?[] { "Proyectos activos", Stats.ActiveProjectCount.ToString() },
            new string?[] { "Tareas abiertas", Stats.OpenTaskCount.ToString() },
            new string?[] { "Tareas finalizadas", Stats.FinishedTaskCount.ToString() },
            new string?[] { "Tareas vencidas", Stats.OverdueTaskCount.ToString() },
            new string?[] { "Seguimientos abiertos", Stats.OpenFollowUpCount.ToString() },
            new string?[] { "Personas", Stats.PeopleCount.ToString() },
            new string?[] { "Empresas", Stats.CompanyCount.ToString() },
            new string?[] { "Adjuntos", Stats.AttachmentCount.ToString() },
            new string?[] { "Comunicaciones (30 d)", Stats.CommunicationsLast30.ToString() },
            new string?[] { "Días medios hasta finalizar", Stats.AvgDaysToFinish.ToString("0.0") },
        };

        try
        {
            var path = await unitOfWork.RunAsync((sp, ct) => sp.GetRequiredService<IReportExporter>().ExportTableAsync(
                new TableExport { BaseName = "estadisticas", Headers = ["Métrica", "Valor"], Rows = rows }, kind, ct));

            ExportMessage = $"Guardado en {path}";
            launcher.RevealInFolder(path);
        }
        catch (Exception ex)
        {
            ExportMessage = $"No se pudo exportar: {ex.Message}";
        }
    }

    private static void Fill(ObservableCollection<BarRow> target, IEnumerable<(string Label, int Count)> items)
    {
        var list = items.ToList();
        var max = list.Count == 0 ? 1 : Math.Max(1, list.Max(x => x.Count));
        target.Clear();
        foreach (var (label, count) in list)
        {
            target.Add(new BarRow(label, count.ToString(), (double)count / max));
        }
    }
}
