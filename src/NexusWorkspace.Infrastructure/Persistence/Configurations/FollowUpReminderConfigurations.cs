using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.FollowUps;
using NexusWorkspace.Domain.Notifications;
using NexusWorkspace.Domain.Reminders;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.ToTable("FollowUps");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Subject).HasMaxLength(500).IsRequired();
        builder.Property(f => f.WaitingOnLabel).HasMaxLength(200);
        builder.Property(f => f.Resolution).HasMaxLength(4000);

        builder.HasOne(f => f.WaitingOnPerson)
            .WithMany()
            .HasForeignKey(f => f.WaitingOnPersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.WaitingOnCompany)
            .WithMany()
            .HasForeignKey(f => f.WaitingOnCompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(f => f.Project)
            .WithMany()
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => new { f.TargetKind, f.TargetId });
        builder.HasIndex(f => f.State);
        builder.HasIndex(f => f.WaitingSinceUtc);
        builder.HasIndex(f => f.NextFollowUpUtc);
        builder.HasIndex(f => f.ProjectId);
        builder.HasIndex(f => f.IsDeleted);
    }
}

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Text).HasMaxLength(1000).IsRequired();

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RemindAtUtc);
        builder.HasIndex(r => new { r.Status, r.Notified, r.RemindAtUtc });
        builder.HasIndex(r => new { r.TargetKind, r.TargetId });
        builder.HasIndex(r => r.IsDeleted);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000);

        builder.HasIndex(n => n.CreatedAtUtc);
        builder.HasIndex(n => new { n.IsRead, n.IsDismissed });
    }
}
