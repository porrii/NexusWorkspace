using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Search;
using NexusWorkspace.Domain.Enums;
using NexusWorkspace.Infrastructure.Persistence;

namespace NexusWorkspace.Infrastructure.Search;

/// <summary>SQLite FTS5-backed global search. Accent-insensitive, prefix-matching, offline.</summary>
public sealed class Fts5SearchService(NexusDbContext db) : ISearchService
{
    public const string CreateTableSql =
        "CREATE VIRTUAL TABLE IF NOT EXISTS SearchIndex USING fts5(" +
        "entity_kind UNINDEXED, entity_id UNINDEXED, navigate_kind UNINDEXED, navigate_id UNINDEXED, " +
        "project_id UNINDEXED, title, body, tokenize = 'unicode61 remove_diacritics 2');";

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int limit = 40, CancellationToken cancellationToken = default)
    {
        var match = BuildMatchQuery(query);
        if (match is null)
        {
            return [];
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var openedHere = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            openedHere = true;
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT entity_kind, entity_id, navigate_kind, navigate_id, project_id, title, " +
                "snippet(SearchIndex, -1, '[', ']', '…', 12) AS snip, " +
                "bm25(SearchIndex, 5.0, 1.0) AS rank " +
                "FROM SearchIndex WHERE SearchIndex MATCH $q ORDER BY rank LIMIT $limit;";
            command.Parameters.Add(new SqliteParameter("$q", match));
            command.Parameters.Add(new SqliteParameter("$limit", Math.Clamp(limit, 1, 200)));

            var hits = new List<SearchHit>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                hits.Add(new SearchHit
                {
                    EntityKind = ParseKind(reader.GetString(0)),
                    EntityId = Guid.Parse(reader.GetString(1)),
                    NavigateKind = ParseKind(reader.GetString(2)),
                    NavigateId = Guid.Parse(reader.GetString(3)),
                    ProjectId = reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
                    Title = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Snippet = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Rank = reader.IsDBNull(7) ? 0d : reader.GetDouble(7),
                });
            }

            return hits;
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync("DELETE FROM SearchIndex;", cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SearchIndex(entity_kind, entity_id, navigate_kind, navigate_id, project_id, title, body) " +
            "SELECT 'Project', Id, 'Project', Id, NULL, Name, COALESCE(Description, '') FROM Projects WHERE IsDeleted = 0;",
            cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SearchIndex(entity_kind, entity_id, navigate_kind, navigate_id, project_id, title, body) " +
            "SELECT 'WorkTask', Id, 'WorkTask', Id, ProjectId, Title, COALESCE(Description, '') FROM WorkTasks WHERE IsDeleted = 0;",
            cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO SearchIndex(entity_kind, entity_id, navigate_kind, navigate_id, project_id, title, body) " +
            "SELECT 'Comment', Id, TargetKind, TargetId, ProjectId, '', Body FROM Comments WHERE IsDeleted = 0;",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var openedHere = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            openedHere = true;
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM SearchIndex;";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is long value ? value : Convert.ToInt64(result);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private Task EnsureTableAsync(CancellationToken cancellationToken)
        => db.Database.ExecuteSqlRawAsync(CreateTableSql, cancellationToken);

    private static string? BuildMatchQuery(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var terms = input
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(Sanitize)
            .Where(term => term.Length > 0)
            .ToList();

        if (terms.Count == 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < terms.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append('"').Append(terms[i]).Append('"');
            if (i == terms.Count - 1)
            {
                builder.Append('*');
            }
        }

        return builder.ToString();
    }

    private static string Sanitize(string term)
        => new(term.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());

    private static EntityKind ParseKind(string value)
        => Enum.TryParse<EntityKind>(value, out var kind) ? kind : EntityKind.Project;
}
