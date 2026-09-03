using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.Activity;
using NexusWorkspace.Domain.Collaboration;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body).IsRequired();
        builder.Property(c => c.AuthorLabel).HasMaxLength(120).IsRequired();

        builder.HasIndex(c => new { c.TargetKind, c.TargetId });
        builder.HasIndex(c => c.ProjectId);
        builder.HasIndex(c => c.IsDeleted);
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).HasMaxLength(260).IsRequired();
        builder.Property(a => a.RelativePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(180);
        builder.Property(a => a.ContentHash).HasMaxLength(64);
        builder.Property(a => a.ThumbnailPath).HasMaxLength(500);

        builder.HasIndex(a => new { a.TargetKind, a.TargetId });
        builder.HasIndex(a => a.ProjectId);
        builder.HasIndex(a => a.ContentHash);
        builder.HasIndex(a => a.IsDeleted);
    }
}

internal sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("ActivityEvents");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Summary).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.OldValue).HasMaxLength(1000);
        builder.Property(a => a.NewValue).HasMaxLength(1000);
        builder.Property(a => a.Note).HasMaxLength(4000);
        builder.Property(a => a.ActorLabel).HasMaxLength(120).IsRequired();

        // Append-only: the app never issues UPDATE or DELETE against this table.
        builder.HasIndex(a => new { a.TargetKind, a.TargetId, a.OccurredAtUtc });
        builder.HasIndex(a => new { a.ProjectId, a.OccurredAtUtc });
        builder.HasIndex(a => a.OccurredAtUtc);
        builder.HasIndex(a => a.Type);
    }
}
