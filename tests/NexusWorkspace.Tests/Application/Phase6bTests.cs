using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Export;
using NexusWorkspace.Application.Import;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class Phase6bTests
{
    private static byte[] Head(string path, int n)
    {
        using var s = File.OpenRead(path);
        var buffer = new byte[n];
        var read = s.Read(buffer, 0, n);
        return buffer[..read];
    }

    [Fact]
    public async Task Table_export_writes_a_real_pdf_and_xlsx()
    {
        await using var h = new TestHarness();
        await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Alpha" });
        var table = ReportDataService.ProjectsTable(await h.ProjectReads.GetListAsync(ProjectListScope.All));

        var pdf = await h.Exporter.ExportTableAsync(table, ExportFormat.Pdf);
        pdf.Should().EndWith(".pdf");
        System.Text.Encoding.ASCII.GetString(Head(pdf, 4)).Should().Be("%PDF");

        var xlsx = await h.Exporter.ExportTableAsync(table, ExportFormat.Excel);
        xlsx.Should().EndWith(".xlsx");
        Head(xlsx, 2).Should().Equal((byte)'P', (byte)'K'); // xlsx is a zip
        new FileInfo(xlsx).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Project_report_exports_to_pdf_and_excel()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Informe 6b" });
        await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "T1" });

        var data = await h.ReportData.BuildProjectReportAsync(project.Value);
        data.Should().NotBeNull();

        var pdf = await h.Exporter.ExportProjectReportAsync(data!, ExportFormat.Pdf);
        System.Text.Encoding.ASCII.GetString(Head(pdf, 4)).Should().Be("%PDF");

        var xlsx = await h.Exporter.ExportProjectReportAsync(data!, ExportFormat.Excel);
        File.Exists(xlsx).Should().BeTrue();
        new FileInfo(xlsx).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Csv_import_maps_columns_groups_by_project_and_parses_priority_and_checklist()
    {
        using var h = new TestHarness();
        const string csv = """
        Proyecto,Tarea,Prioridad,Pasos
        Portal,Configurar API,High,contrato;pruebas
        Portal,Desplegar,Critical,
        Extranet,Alta usuarios,,
        """;

        var headers = h.Import.DetectCsvHeaders(csv, ",");
        headers.Should().BeEquivalentTo(["Proyecto", "Tarea", "Prioridad", "Pasos"]);

        var map = new CsvImportMap
        {
            ProjectColumn = "Proyecto",
            TaskColumn = "Tarea",
            PriorityColumn = "Prioridad",
            ChecklistColumn = "Pasos",
        };

        var parsed = h.Import.Parse(ImportSource.Csv, csv, map);
        parsed.IsSuccess.Should().BeTrue();

        var preview = parsed.Value;
        preview.ProjectCount.Should().Be(2);
        var portal = preview.Projects.Single(p => p.Name == "Portal");
        portal.Tasks.Should().HaveCount(2);
        portal.Tasks[0].Priority.Should().Be(Priority.High);
        portal.Tasks[0].Checklist.Should().BeEquivalentTo(["contrato", "pruebas"]);
        portal.Tasks[1].Priority.Should().Be(Priority.Critical);
        preview.Projects.Single(p => p.Name == "Extranet").Tasks.Should().ContainSingle();
    }

    [Fact]
    public async Task Csv_import_without_project_column_lands_in_one_project_and_commits()
    {
        await using var h = new TestHarness();
        const string csv = "Titulo\nUno\nDos\nTres\n";

        var parsed = h.Import.Parse(ImportSource.Csv, csv, new CsvImportMap { TaskColumn = "Titulo" });
        parsed.IsSuccess.Should().BeTrue();
        parsed.Value.Projects.Should().ContainSingle().Which.Tasks.Should().HaveCount(3);

        var committed = await h.Import.CommitAsync(parsed.Value);
        committed.IsSuccess.Should().BeTrue();
        (await h.Db.WorkTasks.CountAsync()).Should().Be(3);
        (await h.Db.Projects.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Csv_import_needs_a_task_column()
    {
        using var h = new TestHarness();
        var parsed = h.Import.Parse(ImportSource.Csv, "A,B\n1,2\n", new CsvImportMap());
        parsed.IsFailure.Should().BeTrue();
    }
}
