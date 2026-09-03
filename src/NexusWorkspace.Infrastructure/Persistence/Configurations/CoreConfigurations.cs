using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(8000);
        builder.Property(p => p.Icon).HasMaxLength(64);
        builder.Property(p => p.Color).HasMaxLength(9);

        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerPersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IsArchived);
        builder.HasIndex(p => p.IsDeleted);
        builder.HasIndex(p => p.LastOpenedAtUtc);
    }
}

internal sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("WorkTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(20000);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneePersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.RelatedCompany)
            .WithMany()
            .HasForeignKey(t => t.RelatedCompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DueDateUtc);
        builder.HasIndex(t => new { t.ProjectId, t.SortKey });
        builder.HasIndex(t => t.IsArchived);
        builder.HasIndex(t => t.IsDeleted);
    }
}

internal sealed class SubTaskConfiguration : IEntityTypeConfiguration<SubTask>
{
    public void Configure(EntityTypeBuilder<SubTask> builder)
    {
        builder.ToTable("SubTasks");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).HasMaxLength(300).IsRequired();

        builder.HasOne(s => s.WorkTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(s => s.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Parent)
            .WithMany(s => s.Children)
            .HasForeignKey(s => s.ParentSubTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.WorkTaskId, s.ParentSubTaskId, s.SortKey });
    }
}

internal sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).HasMaxLength(500).IsRequired();

        builder.HasOne(c => c.WorkTask)
            .WithMany(t => t.Checklist)
            .HasForeignKey(c => c.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.WorkTaskId, c.SortKey });
    }
}

internal sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("TaskDependencies");
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.WorkTask)
            .WithMany(t => t.DependsOn)
            .HasForeignKey(d => d.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.DependsOnWorkTask)
            .WithMany(t => t.Dependents)
            .HasForeignKey(d => d.DependsOnWorkTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.WorkTaskId, d.DependsOnWorkTaskId }).IsUnique();
    }
}
