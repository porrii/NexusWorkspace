using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.Communications;
using NexusWorkspace.Domain.Meetings;
using NexusWorkspace.Domain.Relations;
using NexusWorkspace.Domain.SavedSearches;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> builder)
    {
        builder.ToTable("Communications");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Subject).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Body).HasMaxLength(20000);
        builder.Property(c => c.ContactLabel).HasMaxLength(200);

        builder.HasOne(c => c.Person).WithMany()
            .HasForeignKey(c => c.PersonId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.Company).WithMany()
            .HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.Project).WithMany()
            .HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.WorkTask).WithMany()
            .HasForeignKey(c => c.WorkTaskId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.OccurredAtUtc);
        builder.HasIndex(c => c.PersonId);
        builder.HasIndex(c => c.CompanyId);
        builder.HasIndex(c => c.ProjectId);
        builder.HasIndex(c => c.WorkTaskId);
        builder.HasIndex(c => c.IsDeleted);
    }
}

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Agenda).HasMaxLength(8000);
        builder.Property(m => m.Notes).HasMaxLength(20000);
        builder.Property(m => m.Location).HasMaxLength(300);

        builder.HasOne(m => m.Project).WithMany()
            .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.StartUtc);
        builder.HasIndex(m => m.ProjectId);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.IsDeleted);
    }
}

internal sealed class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("MeetingParticipants");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ExternalName).HasMaxLength(200);
        builder.Property(p => p.Role).HasMaxLength(200);

        builder.HasOne(p => p.Meeting).WithMany(m => m.Participants)
            .HasForeignKey(p => p.MeetingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Person).WithMany()
            .HasForeignKey(p => p.PersonId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.MeetingId, p.PersonId });
        builder.HasIndex(p => p.PersonId);
    }
}

internal sealed class EntityRelationConfiguration : IEntityTypeConfiguration<EntityRelation>
{
    public void Configure(EntityTypeBuilder<EntityRelation> builder)
    {
        builder.ToTable("EntityRelations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Note).HasMaxLength(1000);

        builder.HasIndex(r => new { r.FromKind, r.FromId });
        builder.HasIndex(r => new { r.ToKind, r.ToId });
        builder.HasIndex(r => r.IsDeleted);
    }
}

internal sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("SavedSearches");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(120).IsRequired();
        builder.Property(s => s.QueryText).HasMaxLength(500);
        builder.Property(s => s.FiltersJson).HasMaxLength(8000);

        builder.HasIndex(s => new { s.Kind, s.SortKey });
        builder.HasIndex(s => s.IsPinned);
    }
}
