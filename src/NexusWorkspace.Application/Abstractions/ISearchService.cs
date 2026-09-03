using NexusWorkspace.Application.Search;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Global full-text search over projects, tasks and comments. Backed by SQLite
/// FTS5, kept in sync on every save. Instant, offline, accent-insensitive.
/// </summary>
public interface ISearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int limit = 40, CancellationToken cancellationToken = default);

    /// <summary>Rebuilds the whole index from the current data. Cheap; safe to call anytime.</summary>
    Task RebuildAsync(CancellationToken cancellationToken = default);
}
