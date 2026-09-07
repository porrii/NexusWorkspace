using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexusWorkspace.Infrastructure.Export;

/// <summary>
/// Writes reports to <see cref="IAppPaths.ExportsDirectory"/> as CSV (RFC-4180),
/// JSON (indented), Excel (ClosedXML) or PDF (QuestPDF, Community licence).
/// </summary>
public sealed class ReportExporter(IAppPaths paths) : IReportExporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    static ReportExporter() => QuestPDF.Settings.License = LicenseType.Community;

    public async Task<string> ExportTableAsync(TableExport table, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var path = NewPath(table.BaseName, format);

        switch (format)
        {
            case ExportFormat.Json:
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(ToDictionaries(table), Json), cancellationToken).ConfigureAwait(false);
                break;

            case ExportFormat.Excel:
                await Task.Run(() => WriteWorkbook(path, wb => AddSheet(wb, table.BaseName, table.Headers, table.Rows)), cancellationToken).ConfigureAwait(false);
                break;

            case ExportFormat.Pdf:
                await Task.Run(() => WritePdf(path, table.BaseName, col =>
                {
                    col.Item().Text(table.BaseName).FontSize(18).SemiBold();
                    col.Item().Text($"Generado {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Element(c => Table(c, table.Headers, table.Rows));
                }), cancellationToken).ConfigureAwait(false);
                break;

            default:
                await WriteCsvAsync(path, table.Headers, table.Rows, cancellationToken).ConfigureAwait(false);
                break;
        }

        return path;
    }

    public async Task<string> ExportProjectReportAsync(ProjectReportData data, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var baseName = $"informe-{Slug(data.ProjectName)}";
        var path = NewPath(baseName, format);

        switch (format)
        {
            case ExportFormat.Json:
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, Json), cancellationToken).ConfigureAwait(false);
                break;

            case ExportFormat.Excel:
                await Task.Run(() => WriteWorkbook(path, wb =>
                {
                    var summary = wb.Worksheets.Add("Resumen");
                    string[,] meta =
                    {
                        { "Proyecto", data.ProjectName },
                        { "Estado", data.Status },
                        { "Prioridad", data.Priority },
                        { "Inicio", data.StartDateUtc?.ToString("yyyy-MM-dd") ?? "" },
                        { "Vencimiento", data.DueDateUtc?.ToString("yyyy-MM-dd") ?? "" },
                        { "Progreso", data.ProgressPercent + "%" },
                        { "Generado", data.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm") + " UTC" },
                    };
                    for (var r = 0; r < meta.GetLength(0); r++)
                    {
                        summary.Cell(r + 1, 1).Value = meta[r, 0];
                        summary.Cell(r + 1, 1).Style.Font.Bold = true;
                        summary.Cell(r + 1, 2).Value = meta[r, 1];
                    }

                    summary.Columns().AdjustToContents();

                    AddSheet(wb, "Tareas",
                        ["Título", "Estado", "Prioridad", "Vencimiento", "Responsable"],
                        data.Tasks.Select(t => (IReadOnlyList<string?>)new[] { t.Title, t.Status, t.Priority, t.Due, t.Assignee }).ToList());
                    AddListSheet(wb, "Seguimientos", data.FollowUps);
                    AddListSheet(wb, "Comunicaciones", data.Communications);
                    AddListSheet(wb, "Cronología", data.Timeline);
                }), cancellationToken).ConfigureAwait(false);
                break;

            case ExportFormat.Pdf:
                await Task.Run(() => WritePdf(path, $"Informe · {data.ProjectName}", col =>
                {
                    col.Item().Text(data.ProjectName).FontSize(20).Bold();
                    col.Item().Text($"{data.Status} · {data.Priority} · {data.ProgressPercent}% completado").FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Inicio {data.StartDateUtc:dd/MM/yyyy}   Vencimiento {data.DueDateUtc:dd/MM/yyyy}   Generado {data.GeneratedAtUtc:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(9).FontColor(Colors.Grey.Medium);

                    if (!string.IsNullOrWhiteSpace(data.Description))
                    {
                        col.Item().PaddingTop(8).Text(data.Description!).FontSize(10);
                    }

                    Section(col, "Tareas");
                    col.Item().Element(c => Table(c,
                        ["Título", "Estado", "Prioridad", "Vencimiento", "Responsable"],
                        data.Tasks.Select(t => (IReadOnlyList<string?>)new[] { t.Title, t.Status, t.Priority, t.Due, t.Assignee }).ToList()));

                    PdfList(col, "Seguimientos", data.FollowUps);
                    PdfList(col, "Comunicaciones", data.Communications);
                    PdfList(col, "Cronología", data.Timeline);
                }), cancellationToken).ConfigureAwait(false);
                break;

            default:
                await WriteProjectCsvAsync(path, data, cancellationToken).ConfigureAwait(false);
                break;
        }

        return path;
    }

    // ---------- CSV ----------

    private static async Task WriteCsvAsync(string path, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(Csv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(Csv)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteProjectCsvAsync(string path, ProjectReportData data, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Informe de proyecto: {data.ProjectName}");
        sb.AppendLine($"Estado,{data.Status}");
        sb.AppendLine($"Prioridad,{data.Priority}");
        sb.AppendLine($"Progreso,{data.ProgressPercent}%");
        sb.AppendLine();
        sb.AppendLine("Título,Estado,Prioridad,Vencimiento,Responsable");
        foreach (var t in data.Tasks)
        {
            sb.AppendLine(string.Join(',', new[] { t.Title, t.Status, t.Priority, t.Due, t.Assignee }.Select(Csv)));
        }

        AppendCsvList(sb, "Seguimientos", data.FollowUps);
        AppendCsvList(sb, "Comunicaciones", data.Communications);
        AppendCsvList(sb, "Cronología", data.Timeline);

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true), cancellationToken).ConfigureAwait(false);
    }

    private static void AppendCsvList(StringBuilder sb, string heading, IReadOnlyList<string> items)
    {
        sb.AppendLine();
        sb.AppendLine(heading);
        foreach (var item in items)
        {
            sb.AppendLine(Csv(item));
        }
    }

    // ---------- Excel ----------

    private static void WriteWorkbook(string path, Action<XLWorkbook> build)
    {
        using var workbook = new XLWorkbook();
        build(workbook);
        if (workbook.Worksheets.Count == 0)
        {
            workbook.Worksheets.Add("Datos");
        }

        workbook.SaveAs(path);
    }

    private static void AddSheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var sheet = workbook.Worksheets.Add(SafeSheetName(name));
        for (var c = 0; c < headers.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
            sheet.Cell(1, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < headers.Count; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = c < rows[r].Count ? rows[r][c] ?? string.Empty : string.Empty;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    private static void AddListSheet(XLWorkbook workbook, string name, IReadOnlyList<string> items)
    {
        var sheet = workbook.Worksheets.Add(SafeSheetName(name));
        for (var r = 0; r < items.Count; r++)
        {
            sheet.Cell(r + 1, 1).Value = items[r];
        }

        sheet.Columns().AdjustToContents();
    }

    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(c => c is not ('[' or ']' or '*' or '?' or '/' or '\\' or ':')).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    // ---------- PDF ----------

    private static void WritePdf(string path, string title, Action<ColumnDescriptor> content)
    {
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(s => s.FontSize(10));
                page.Header().Text(title).FontSize(9).FontColor(Colors.Grey.Medium);
                page.Content().Column(content);
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("NexusWorkspace · ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(path);
    }

    private static void Section(ColumnDescriptor column, string title)
        => column.Item().PaddingTop(14).Text(title).FontSize(13).SemiBold();

    private static void Table(IContainer container, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (var head in headers)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(head).SemiBold().FontSize(9);
                }
            });

            foreach (var row in rows)
            {
                for (var c = 0; c < headers.Count; c++)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(c < row.Count ? row[c] ?? string.Empty : string.Empty).FontSize(9);
                }
            }
        });
    }

    private static void PdfList(ColumnDescriptor column, string title, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(14).Text(title).FontSize(13).SemiBold();
        foreach (var item in items)
        {
            column.Item().PaddingLeft(6).Text("• " + item).FontSize(9);
        }
    }

    // ---------- shared ----------

    private static object ToDictionaries(TableExport table) => new
    {
        title = table.BaseName,
        generatedAtUtc = DateTime.UtcNow,
        rows = table.Rows.Select(r => table.Headers
            .Select((h, i) => new { h, v = i < r.Count ? r[i] : null })
            .ToDictionary(x => x.h, x => x.v)),
    };

    private string NewPath(string baseName, ExportFormat format)
    {
        Directory.CreateDirectory(paths.ExportsDirectory);
        var ext = format switch
        {
            ExportFormat.Json => "json",
            ExportFormat.Excel => "xlsx",
            ExportFormat.Pdf => "pdf",
            _ => "csv",
        };
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
        var chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return string.IsNullOrEmpty(slug) ? "proyecto" : slug;
    }
}
