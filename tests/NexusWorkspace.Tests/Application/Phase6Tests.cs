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

public class Phase6Tests
{
    // ---------- Statistics ----------

    [Fact]
    public async Task Stats_counts_open_finished_and_overdue_tasks()
    {
        await using var h = new TestHarness();
        var p = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        var open = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = p.Value, Title = "Abierta" });
        var overdue = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = p.Value, Title = "Vencida" });
        var done = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = p.Value, Title = "Hecha" });

        await h.Tasks.UpdateDetailsAsync(new UpdateWorkTaskDetailsRequest { Id = overdue.Value, DueDateUtc = h.Clock.UtcNow.AddDays(-2) });
        await h.Tasks.ChangeStatusAsync(done.Value, WorkTaskStatus.InProgress);
        await h.Tasks.ChangeStatusAsync(done.Value, WorkTaskStatus.Finished);

        var stats = await h.Stats.GetAsync();
        stats.TotalTaskCount.Should().Be(3);
        stats.FinishedTaskCount.Should().Be(1);
        stats.OpenTaskCount.Should().Be(2);
        stats.OverdueTaskCount.Should().Be(1);
        stats.ActiveProjectCount.Should().Be(1);
        stats.TasksByStatus.Should().Contain(x => x.Name == "Finalizada" && x.Count == 1);
        stats.FinishedPerWeek.Should().HaveCount(12);
    }

    // ---------- Templates ----------

    [Fact]
    public async Task Project_template_round_trips_tasks_subtasks_checklist_and_tags()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Origen", Description = "desc" });
        var task = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Integración", Priority = Priority.High });
        var sub = await h.Tasks.AddSubTaskAsync(task.Value, "Revisar API");
        await h.Tasks.AddSubTaskAsync(task.Value, "Probar", sub.Value);
        await h.Tasks.AddChecklistItemAsync(task.Value, "DESA");
        var tag = await h.Tags.CreateAsync(new NexusWorkspace.Application.Tags.CreateTagRequest { Name = "SIP" });
        await h.Db.WorkTaskTags.AddAsync(new NexusWorkspace.Domain.Tasks.WorkTaskTag { WorkTaskId = task.Value, TagId = tag.Value });
        await h.Db.SaveChangesAsync();

        var template = await h.Templates.CreateFromProjectAsync(project.Value, "Plantilla SIP");
        template.IsSuccess.Should().BeTrue();

        var applied = await h.Templates.ApplyProjectTemplateAsync(template.Value, "Nuevo desde plantilla");
        applied.IsSuccess.Should().BeTrue();

        var newProjectId = applied.Value;
        var newTasks = await h.Db.WorkTasks.Where(t => t.ProjectId == newProjectId).ToListAsync();
        newTasks.Should().ContainSingle().Which.Title.Should().Be("Integración");
        newTasks[0].Priority.Should().Be(Priority.High);

        var newSubs = await h.Db.SubTasks.Where(s => s.WorkTaskId == newTasks[0].Id).ToListAsync();
        newSubs.Should().HaveCount(2);
        newSubs.Count(s => s.ParentSubTaskId != null).Should().Be(1);

        (await h.Db.ChecklistItems.CountAsync(c => c.WorkTaskId == newTasks[0].Id)).Should().Be(1);
        (await h.Db.WorkTaskTags.CountAsync(x => x.WorkTaskId == newTasks[0].Id)).Should().Be(1);
        (await h.Db.Tags.CountAsync(t => t.Name == "SIP")).Should().Be(1, "the tag is reused, not duplicated");

        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.TemplateApplied
            && e.TargetKind == EntityKind.Project && e.TargetId == newProjectId)).Should().Be(1);

        var list = await h.TemplateReads.GetListAsync(TemplateKind.Project);
        list.Should().ContainSingle().Which.UseCount.Should().Be(1);
    }

    [Fact]
    public async Task Task_template_adds_a_task_with_its_subtree_to_an_existing_project()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });
        var task = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "Despliegue", Priority = Priority.Critical });
        await h.Tasks.AddSubTaskAsync(task.Value, "PRE");
        await h.Tasks.AddChecklistItemAsync(task.Value, "Backup");

        var template = await h.Templates.CreateFromTaskAsync(task.Value, "Despliegue estándar");
        var applied = await h.Templates.ApplyTaskTemplateAsync(template.Value, project.Value);
        applied.IsSuccess.Should().BeTrue();

        var newTask = await h.Db.WorkTasks.SingleAsync(t => t.Id == applied.Value);
        newTask.Title.Should().Be("Despliegue estándar");
        newTask.Priority.Should().Be(Priority.Critical);
        (await h.Db.SubTasks.CountAsync(s => s.WorkTaskId == newTask.Id)).Should().Be(1);
        (await h.Db.ChecklistItems.CountAsync(c => c.WorkTaskId == newTask.Id)).Should().Be(1);
    }

    // ---------- Import ----------

    [Fact]
    public async Task Notes_import_builds_a_project_task_subtask_tree_with_priority_markers()
    {
        await using var h = new TestHarness();
        var text = string.Join('\n',
            "# Migración correo",
            "Configurar buzones!",
            "    Alta usuarios",
            "        Importar CSV",
            "    - [ ] Revisar cuotas",
            "Pruebas??");

        var parsed = h.Import.Parse(ImportSource.Notes, text);
        parsed.IsSuccess.Should().BeTrue();

        var preview = parsed.Value;
        preview.ProjectCount.Should().Be(1);
        var project = preview.Projects[0];
        project.Name.Should().Be("Migración correo");
        project.Tasks.Should().HaveCount(2);
        project.Tasks[0].Title.Should().Be("Configurar buzones");
        project.Tasks[0].Priority.Should().Be(Priority.High);
        project.Tasks[0].SubTasks.Should().ContainSingle().Which.Title.Should().Be("Alta usuarios");
        project.Tasks[0].SubTasks[0].SubTasks.Should().ContainSingle().Which.Title.Should().Be("Importar CSV");
        project.Tasks[0].Checklist.Should().ContainSingle().Which.Should().Be("Revisar cuotas");
        project.Tasks[1].Priority.Should().Be(Priority.Low);

        var committed = await h.Import.CommitAsync(preview);
        committed.IsSuccess.Should().BeTrue();
        committed.Value.Should().Be(1);

        (await h.Db.Projects.CountAsync(p => p.Name == "Migración correo")).Should().Be(1);
        (await h.Db.WorkTasks.CountAsync()).Should().Be(2);
        (await h.Db.SubTasks.CountAsync()).Should().Be(2);
        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.Imported)).Should().Be(1);
    }

    [Fact]
    public void Json_import_reads_projects_and_nested_tasks()
    {
        using var h = new TestHarness();
        const string json = """
        { "projects": [
          { "name": "Portal", "description": "web", "tasks": [
            { "title": "API", "priority": "High", "checklist": ["contrato"], "subTasks": [ { "title": "Auth" } ] }
          ] }
        ] }
        """;

        var parsed = h.Import.Parse(ImportSource.Json, json);
        parsed.IsSuccess.Should().BeTrue();
        var project = parsed.Value.Projects.Should().ContainSingle().Subject;
        project.Name.Should().Be("Portal");
        project.Tasks.Should().ContainSingle();
        project.Tasks[0].Priority.Should().Be(Priority.High);
        project.Tasks[0].SubTasks.Should().ContainSingle().Which.Title.Should().Be("Auth");
        project.Tasks[0].Checklist.Should().ContainSingle();
    }

    [Fact]
    public void Bad_json_is_reported_not_thrown()
    {
        using var h = new TestHarness();
        var parsed = h.Import.Parse(ImportSource.Json, "{ not json ");
        parsed.IsFailure.Should().BeTrue();
        parsed.Error.Code.Should().Be("import.bad_json");
    }

    // ---------- Export ----------

    [Fact]
    public async Task Table_export_writes_a_csv_file_with_a_header_row()
    {
        await using var h = new TestHarness();
        await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Alpha, con coma" });

        var projects = await h.ProjectReads.GetListAsync(ProjectListScope.All);
        var table = ReportDataService.ProjectsTable(projects);

        var path = await h.Exporter.ExportTableAsync(table, ExportFormat.Csv);
        File.Exists(path).Should().BeTrue();
        var text = await File.ReadAllTextAsync(path);
        text.Should().StartWith("Nombre,Estado,Prioridad");
        text.Should().Contain("\"Alpha, con coma\"", "commas in values are quoted");
    }

    [Fact]
    public async Task Project_report_json_contains_tasks_and_metadata()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Informe" });
        await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "T1" });

        var data = await h.ReportData.BuildProjectReportAsync(project.Value);
        data.Should().NotBeNull();

        var path = await h.Exporter.ExportProjectReportAsync(data!, ExportFormat.Json);
        var text = await File.ReadAllTextAsync(path);
        text.Should().Contain("\"projectName\": \"Informe\"");
        text.Should().Contain("\"T1\"");
    }
}
