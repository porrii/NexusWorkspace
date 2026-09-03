using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.Companies;
using NexusWorkspace.Domain.People;
using NexusWorkspace.Domain.Projects;
using NexusWorkspace.Domain.Tags;
using NexusWorkspace.Domain.Tasks;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Role).HasMaxLength(200);
        builder.Property(p => p.Email).HasMaxLength(320);
        builder.Property(p => p.Phone).HasMaxLength(60);
        builder.Property(p => p.Notes).HasMaxLength(8000);

        builder.HasOne(p => p.Company)
            .WithMany(c => c.People)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.IsDeleted);
    }
}

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(8000);

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.IsDeleted);
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(60).IsRequired();
        builder.Property(t => t.Color).HasMaxLength(9);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

internal sealed class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    public void Configure(EntityTypeBuilder<ProjectTag> builder)
    {
        builder.ToTable("ProjectTags");
        builder.HasKey(x => new { x.ProjectId, x.TagId });

        builder.HasOne(x => x.Project).WithMany(p => p.Tags)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(t => t.Projects)
            .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkTaskTagConfiguration : IEntityTypeConfiguration<WorkTaskTag>
{
    public void Configure(EntityTypeBuilder<WorkTaskTag> builder)
    {
        builder.ToTable("WorkTaskTags");
        builder.HasKey(x => new { x.WorkTaskId, x.TagId });

        builder.HasOne(x => x.WorkTask).WithMany(t => t.Tags)
            .HasForeignKey(x => x.WorkTaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(t => t.Tasks)
            .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProjectPersonConfiguration : IEntityTypeConfiguration<ProjectPerson>
{
    public void Configure(EntityTypeBuilder<ProjectPerson> builder)
    {
        builder.ToTable("ProjectPeople");
        builder.HasKey(x => new { x.ProjectId, x.PersonId });

        builder.Property(x => x.Role).HasMaxLength(200);

        builder.HasOne(x => x.Project).WithMany(p => p.People)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Person).WithMany()
            .HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProjectCompanyConfiguration : IEntityTypeConfiguration<ProjectCompany>
{
    public void Configure(EntityTypeBuilder<ProjectCompany> builder)
    {
        builder.ToTable("ProjectCompanies");
        builder.HasKey(x => new { x.ProjectId, x.CompanyId });

        builder.HasOne(x => x.Project).WithMany(p => p.Companies)
            .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WorkTaskPersonConfiguration : IEntityTypeConfiguration<WorkTaskPerson>
{
    public void Configure(EntityTypeBuilder<WorkTaskPerson> builder)
    {
        builder.ToTable("WorkTaskPeople");
        builder.HasKey(x => new { x.WorkTaskId, x.PersonId });

        builder.HasOne(x => x.WorkTask).WithMany(t => t.People)
            .HasForeignKey(x => x.WorkTaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Person).WithMany()
            .HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}
