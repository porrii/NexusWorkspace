using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Domain.Inbox;

namespace NexusWorkspace.Application.Inbox;

public sealed class InboxReadService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<InboxItemView>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await db.InboxItems.AsNoTracking()
            .Where(i => i.State == InboxItemState.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new InboxItemView
            {
                Id = i.Id,
                RawText = i.RawText,
                ParsedHint = i.ParsedHint,
                CreatedAtUtc = i.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
        => db.InboxItems.AsNoTracking().CountAsync(i => i.State == InboxItemState.Pending, cancellationToken);
}
