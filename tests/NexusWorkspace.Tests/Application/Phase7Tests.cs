using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Application.Trash;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Infrastructure.Backups;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class Phase7Tests
{
    // ---------- Backups ----------

    private static (FileSystemBackupService Service, TempPaths Paths) NewBackup()
    {
        var paths = new TempPaths();
        File.WriteAllText(paths.DatabasePath, "db-v1");
        File.WriteAllText(paths.SettingsFilePath, "{\"schemaVersion\":1}");
        return (new FileSystemBackupService(paths, NullLogger<FileSystemBackupService>.Instance), paths);
    }

    [Fact]
    public async Task Create_then_list_reports_the_archive()
    {
        var (backup, paths) = NewBackup();
        using var _ = paths;

        var info = await backup.CreateAsync("manual");
        info.Reason.Should().Be("manual");
        info.SizeBytes.Should().BeGreaterThan(0);

        var list = await backup.ListAsync();
        list.Should().ContainSingle().Which.Id.Should().Be(info.Id);
        File.Exists(Path.Combine(paths.BackupsDirectory, info.FileName)).Should().BeTrue();
    }

    [Fact]
    public async Task Staged_restore_is_applied_on_next_start_and_brings_the_db_back()
    {
        var (backup, paths) = NewBackup();
        using var _ = paths;

        var info = await backup.CreateAsync("manual");

        File.WriteAllText(paths.DatabasePath, "db-v2-corrupted");
        await backup.StageRestoreAsync(info.Id);
        File.Exists(Path.Combine(paths.RootDirectory, ".nexus-restore")).Should().BeTrue();
        backup.RestartPending.Should().BeTrue();

        var applied = await backup.ApplyStagedAsync();
        applied.Should().BeTrue();
        File.ReadAllText(paths.DatabasePath).Should().Be("db-v1");
        File.Exists(Path.Combine(paths.RootDirectory, ".nexus-restore")).Should().BeFalse();

        // A safety "prerestore" copy was taken before overwriting.
        (await backup.ListAsync()).Should().Contain(b => b.Reason == "prerestore");
    }

    [Fact]
    public async Task Prune_keeps_the_newest_n_archives()
    {
        var (backup, paths) = NewBackup();
        using var _ = paths;

        for (var i = 0; i < 4; i++)
        {
            await backup.CreateAsync($"r{i}");
            await Task.Delay(1100); // filenames are second-resolution
        }

        var removed = await backup.PruneAsync(2);
        removed.Should().Be(2);
        (await backup.ListAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Export_and_staged_import_replace_the_workspace()
    {
        var (backup, paths) = NewBackup();
        using var _ = paths;

        var exportPath = Path.Combine(paths.ExportsDirectory, "ws.zip");
        Directory.CreateDirectory(paths.ExportsDirectory);
        await backup.ExportToAsync(exportPath, includeFiles: false);
        File.Exists(exportPath).Should().BeTrue();

        File.WriteAllText(paths.DatabasePath, "changed");
        await backup.StageImportAsync(exportPath);
        (await backup.ApplyStagedAsync()).Should().BeTrue();
        File.ReadAllText(paths.DatabasePath).Should().Be("db-v1");
    }

    [Fact]
    public async Task Apply_staged_is_a_no_op_when_nothing_is_pending()
    {
        var (backup, paths) = NewBackup();
        using var _ = paths;
        (await backup.ApplyStagedAsync()).Should().BeFalse();
    }

    // ---------- Trash / archived ----------

    [Fact]
    public async Task Trashed_project_is_listed_restored_and_finally_purged()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Temporal" });
        await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "T" });

        await h.Projects.TrashAsync(project.Value);

        var trashed = await h.TrashReads.GetAsync(TrashScope.Trashed);
        trashed.Should().Contain(e => e.Kind == EntityKind.Project && e.Id == project.Value && e.IsDeleted);

        // Restore brings it back into the normal (filtered) query.
        (await h.Trash.RestoreAsync(EntityKind.Project, project.Value)).IsSuccess.Should().BeTrue();
        (await h.Db.Projects.CountAsync(p => p.Id == project.Value)).Should().Be(1);

        // Trash again, then purge for good — the child task cascades.
        await h.Projects.TrashAsync(project.Value);
        (await h.Trash.PurgeAsync(EntityKind.Project, project.Value)).IsSuccess.Should().BeTrue();
        (await h.Db.Projects.IgnoreQueryFilters().CountAsync(p => p.Id == project.Value)).Should().Be(0);
        (await h.Db.WorkTasks.IgnoreQueryFilters().CountAsync(t => t.ProjectId == project.Value)).Should().Be(0);

        // History keeps a tombstone.
        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.Deleted && e.TargetId == project.Value))
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Archived_scope_lists_archived_but_not_trashed()
    {
        await using var h = new TestHarness();
        var archived = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Archivado" });
        var trashed = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "Papelera" });

        await h.Projects.ArchiveAsync(archived.Value);
        await h.Projects.TrashAsync(trashed.Value);

        var onlyArchived = await h.TrashReads.GetAsync(TrashScope.Archived);
        onlyArchived.Should().ContainSingle().Which.Id.Should().Be(archived.Value);

        var both = await h.TrashReads.GetAsync(TrashScope.Both);
        both.Select(e => e.Id).Should().BeEquivalentTo([archived.Value, trashed.Value]);
    }

    [Fact]
    public async Task Empty_trash_purges_every_soft_deleted_row()
    {
        await using var h = new TestHarness();
        var a = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "A" });
        var b = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "B" });
        await h.Projects.TrashAsync(a.Value);
        await h.Projects.TrashAsync(b.Value);

        var removed = await h.Trash.EmptyTrashAsync();
        removed.Should().Be(2);
        (await h.Db.Projects.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }
}
