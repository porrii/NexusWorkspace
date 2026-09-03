using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusWorkspace.Domain.Inbox;

namespace NexusWorkspace.Infrastructure.Persistence.Configurations;

internal sealed class InboxItemConfiguration : IEntityTypeConfiguration<InboxItem>
{
    public void Configure(EntityTypeBuilder<InboxItem> builder)
    {
        builder.ToTable("InboxItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.RawText).HasMaxLength(4000).IsRequired();
        builder.Property(i => i.ParsedHint).HasMaxLength(1000);

        builder.HasIndex(i => i.State);
        builder.HasIndex(i => i.CreatedAtUtc);
        builder.HasIndex(i => i.IsDeleted);
    }
}
