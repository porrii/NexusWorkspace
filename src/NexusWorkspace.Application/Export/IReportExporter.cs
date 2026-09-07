namespace NexusWorkspace.Application.Export;

/// <summary>
/// Writes a report to a file under the workspace <c>exports/</c> directory and
/// returns its absolute path. Implemented in Infrastructure.
/// </summary>
public interface IReportExporter
{
    Task<string> ExportTableAsync(TableExport table, ExportFormat format, CancellationToken cancellationToken = default);

    Task<string> ExportProjectReportAsync(ProjectReportData data, ExportFormat format, CancellationToken cancellationToken = default);
}
