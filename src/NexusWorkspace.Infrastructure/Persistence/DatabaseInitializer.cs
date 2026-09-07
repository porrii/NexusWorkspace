using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusWorkspace.Application.Abstractions;
using NexusWorkspace.Application.Settings;
using NexusWorkspace.Infrastructure.Search;

namespace NexusWorkspace.Infrastructure.Persistence;

/// <summary>
/// Prepares the database at startup. Applies pending EF Core migrations when any
/// exist; falls back to <c>EnsureCreated</c> only while the project has no
/// migrations yet (early development). Enables WAL and builds the search index.
/// </summary>
public sealed class DatabaseInitializer(
    NexusDbContext db,
    Fts5SearchService search,
    IBackupService backup,
    ISettingsStore settings,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var definedMigrations = db.Database.GetMigrations().ToList();

        if (definedMigrations.Count == 0)
        {
            logger.LogWarning(
                "No hay migraciones EF definidas. Creando el esquema con EnsureCreated. " +
                "Ejecuta 'dotnet ef migrations add Initial' antes de distribuir.");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation(
                    "Aplicando {Count} migración(es): {Migrations}",
                    pending.Count,
                    string.Join(", ", pending));

                if (settings.Current.Backup.BackupBeforeMigration)
                {
                    try
                    {
                        await backup.CreateAsync("premigration", includeFiles: false, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "No se pudo crear la copia previa a la migración; se continúa.");
                    }
                }
            }

            await db.Database.MigrateAsync(cancellationToken);
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo activar el modo WAL de SQLite.");
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(Fts5SearchService.CreateTableSql, cancellationToken);
            if (await search.CountAsync(cancellationToken) == 0)
            {
                logger.LogInformation("Construyendo el índice de búsqueda…");
                await search.RebuildAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo preparar el índice de búsqueda FTS5.");
        }

        logger.LogInformation("Base de datos lista.");
    }
}
