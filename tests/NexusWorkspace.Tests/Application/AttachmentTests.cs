using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Attachments;
using NexusWorkspace.Application.Common;
using NexusWorkspace.Application.Projects;
using NexusWorkspace.Application.Tasks;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Tests.TestSupport;
using Xunit;

namespace NexusWorkspace.Tests.Application;

public class AttachmentTests
{
    private static MemoryStream Bytes(string content) => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task Add_persists_metadata_stores_file_and_logs_history()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        var id = await h.Attachments.AddAsync(EntityKind.Project, project.Value, "informe-final.pdf", Bytes("hello world"));
        id.IsSuccess.Should().BeTrue();

        var row = await h.Db.Attachments.SingleAsync();
        row.FileName.Should().Be("informe-final.pdf");
        row.ProjectId.Should().Be(project.Value);
        row.SizeBytes.Should().Be(11);
        row.ContentHash.Should().NotBeNullOrEmpty();
        h.AttachmentStore.Exists(row.RelativePath).Should().BeTrue();

        (await h.Db.ActivityEvents.CountAsync(e => e.Type == ActivityType.AttachmentAdded)).Should().Be(1);
    }

    [Fact]
    public async Task Identical_content_deduplicates_to_one_physical_file()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });
        var a = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "A" });
        var b = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "B" });

        await h.Attachments.AddAsync(EntityKind.WorkTask, a.Value, "spec.txt", Bytes("same bytes"));
        await h.Attachments.AddAsync(EntityKind.WorkTask, b.Value, "copy.txt", Bytes("same bytes"));

        var rows = await h.Db.Attachments.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(r => r.RelativePath).Distinct().Should().ContainSingle();

        var shardDir = Path.GetDirectoryName(h.AttachmentStore.GetAbsolutePath(rows[0].RelativePath))!;
        Directory.GetFiles(shardDir).Should().HaveCount(1);
    }

    [Fact]
    public async Task Remove_keeps_shared_blob_until_last_reference_goes()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });
        var a = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "A" });
        var b = await h.Tasks.CreateAsync(new CreateWorkTaskRequest { ProjectId = project.Value, Title = "B" });

        var first = await h.Attachments.AddAsync(EntityKind.WorkTask, a.Value, "x.txt", Bytes("shared"));
        var second = await h.Attachments.AddAsync(EntityKind.WorkTask, b.Value, "y.txt", Bytes("shared"));
        var relative = (await h.Db.Attachments.FirstAsync()).RelativePath;

        await h.Attachments.RemoveAsync(first.Value);
        h.AttachmentStore.Exists(relative).Should().BeTrue("another attachment still references the blob");

        await h.Attachments.RemoveAsync(second.Value);
        h.AttachmentStore.Exists(relative).Should().BeFalse("no live attachment references the blob any more");

        (await h.Db.Attachments.IgnoreQueryFilters().CountAsync(x => x.IsDeleted)).Should().Be(2);
    }

    [Fact]
    public async Task Read_service_filters_by_project_and_kind()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        await h.Attachments.AddAsync(EntityKind.Project, project.Value, "diagram.png", Bytes("img"));
        await h.Attachments.AddAsync(EntityKind.Project, project.Value, "notes.pdf", Bytes("doc"));

        var all = await h.AttachmentReads.GetForProjectAsync(project.Value);
        all.Should().HaveCount(2);
        all.Should().OnlyContain(a => a.ProjectName == "P");

        var imagesOnly = await h.AttachmentReads.GetAllAsync(new AttachmentFilter { ImagesOnly = true });
        imagesOnly.Should().ContainSingle().Which.FileName.Should().Be("diagram.png");

        var pdfs = await h.AttachmentReads.GetAllAsync(new AttachmentFilter { Kind = AttachmentKind.Pdf });
        pdfs.Should().ContainSingle().Which.Kind.Should().Be(AttachmentKind.Pdf);
    }

    [Fact]
    public async Task Attachment_filename_is_indexed_for_global_search()
    {
        await using var h = new TestHarness();
        var project = await h.Projects.CreateAsync(new CreateProjectRequest { Name = "P" });

        await h.Attachments.AddAsync(EntityKind.Project, project.Value, "presupuesto-2026.xlsx", Bytes("data"));

        var hits = await h.Search.SearchAsync("presupuesto");
        hits.Should().Contain(x => x.EntityKind == EntityKind.Attachment);
    }
}
