using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MoojPay.Modules.Marketplace.Persistence.SqlMigrations;

/// <summary>
/// Applies raw, hand-written, versioned SQL scripts embedded from
/// <c>Persistence/Sql/NNNN_description.sql</c>, in filename order, tracking which have already
/// run in a small bookkeeping table. This replaces EF Core's generated-migration workflow per the
/// accepted BuildSpec's "raw versioned SQL migrations, not ORM-generated" stack decision.
/// Every script in this module is additive-only (CREATE TABLE IF NOT EXISTS / CREATE INDEX IF
/// NOT EXISTS): no existing table is altered or dropped.
/// </summary>
public sealed class SqlMigrationRunner(NpgsqlDataSource dataSource, ILogger<SqlMigrationRunner> logger)
{
    private const string HistoryTableSql = """
        CREATE TABLE IF NOT EXISTS __marketplace_schema_migrations
        (
            version     text PRIMARY KEY,
            applied_at  timestamptz NOT NULL
        );
        """;

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using (var createHistory = new NpgsqlCommand(HistoryTableSql, connection))
        {
            await createHistory.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var applied = new HashSet<string>();
        await using (var readApplied = new NpgsqlCommand("SELECT version FROM __marketplace_schema_migrations", connection))
        await using (var reader = await readApplied.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                applied.Add(reader.GetString(0));
            }
        }

        foreach (var (version, sql) in LoadEmbeddedScripts())
        {
            if (applied.Contains(version))
            {
                continue;
            }

            logger.LogInformation("Applying marketplace SQL migration {Version}", version);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (var apply = new NpgsqlCommand(sql, connection, transaction))
            {
                await apply.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var record = new NpgsqlCommand(
                "INSERT INTO __marketplace_schema_migrations (version, applied_at) VALUES (@version, now())",
                connection,
                transaction))
            {
                record.Parameters.AddWithValue("version", version);
                await record.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static IEnumerable<(string Version, string Sql)> LoadEmbeddedScripts()
    {
        var assembly = typeof(SqlMigrationRunner).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Persistence.Sql.", StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded SQL script not found: {resourceName}");
            using var streamReader = new StreamReader(stream);
            var sql = streamReader.ReadToEnd();

            var version = ExtractVersion(resourceName);
            yield return (version, sql);
        }
    }

    private static string ExtractVersion(string resourceName)
    {
        const string marker = ".Persistence.Sql.";
        var index = resourceName.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? resourceName : resourceName[(index + marker.Length)..];
    }
}
