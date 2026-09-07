using System.Globalization;
using System.Text;
using System.Text.Json;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Export;

namespace NexusWorkspace.Infrastructure.Export;

/// <summary>
/// Writes reports to <see cref="IAppPaths.ExportsDirectory"/>. First pass: CSV
/// (RFC-4180 quoting) and JSON (indented). PDF/Excel land in a later phase.
/// </summary>
public sealed class ReportExporter(IAppPaths paths) : IReportExporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<string> ExportTableAsync(TableExport table, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var path = NewPath(table.BaseName, format);

        if (format == ExportFormat.Json)
        {
            var payload = new
            {
                title = table.BaseName,
                generatedAtUtc = DateTime.UtcNow,
                headers = table.Headers,
                rows = table.Rows.Select(r => table.Headers
                    .Select((h, i) => new { h, v = i < r.Count ? r[i] : null })
                    .ToDictionary(x => x.h, x => x.v)),
            };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, Json), cancellationToken).ConfigureAwait(false);
            return path;
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', table.Headers.Select(Csv)));
        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(',', row.Select(Csv)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken)
            .ConfigureAwait(false);
        return path;
    }

    public async Task<string> ExportProjectReportAsync(ProjectReportData data, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var baseName = $"informe-{Slug(data.ProjectName)}";
        var path = NewPath(baseName, format);

        if (format == ExportFormat.Json)
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, Json), cancellationToken).ConfigureAwait(false);
            return path;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Informe de proyecto: {data.ProjectName}");
        sb.AppendLine($"Generado,{data.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Estado,{data.Status}");
        sb.AppendLine($"Prioridad,{data.Priority}");
        sb.AppendLine($"Inicio,{data.StartDateUtc:yyyy-MM-dd}");
        sb.AppendLine($"Vencimiento,{data.DueDateUtc:yyyy-MM-dd}");
        sb.AppendLine($"Progreso,{data.ProgressPercent}%");
        sb.AppendLine();
        sb.AppendLine("## Tareas");
        sb.AppendLine("Título,Estado,Prioridad,Vencimiento,Responsable");
        foreach (var t in data.Tasks)
        {
            sb.AppendLine(string.Join(',', new[] { t.Title, t.Status, t.Priority, t.Due, t.Assignee }.Select(Csv)));
        }

        AppendList(sb, "## Seguimientos", data.FollowUps);
        AppendList(sb, "## Comunicaciones", data.Communications);
        AppendList(sb, "## Cronología", data.Timeline);

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken)
            .ConfigureAwait(false);
        return path;
    }

    private static void AppendList(StringBuilder sb, string heading, IReadOnlyList<string> items)
    {
        sb.AppendLine();
        sb.AppendLine(heading);
        foreach (var item in items)
        {
            sb.AppendLine(Csv(item));
        }
    }

    private string NewPath(string baseName, ExportFormat format)
    {
        Directory.CreateDirectory(paths.ExportsDirectory);
        var ext = format == ExportFormat.Json ? "json" : "csv";
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(paths.ExportsDirectory, $"{baseName}-{stamp}.{ext}");
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string Slug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return string.IsNullOrEmpty(slug) ? "proyecto" : slug;
    }
}
