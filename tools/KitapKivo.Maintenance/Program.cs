using System.Reflection;
using Microsoft.Extensions.Configuration;
using Npgsql;

// Explicit maintenance operation; never runs during application startup or migration.
if (args.Length != 1 || args[0] is not ("report" or "apply"))
    throw new ArgumentException("Usage: KitapKivo.Maintenance report|apply");
var apply = args[0] == "apply";
var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.Load("KitapKivo.Api"), optional: true)
    .AddEnvironmentVariables().Build();
await using var connection = new NpgsqlConnection(config.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionStrings:Database is required."));
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();
async Task Execute(string sql)
{
    await using var command = new NpgsqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}
async Task<long> Count(string sql)
{
    await using var command = new NpgsqlCommand(sql, connection, transaction);
    return Convert.ToInt64(await command.ExecuteScalarAsync());
}
var tables = new (string Schema, string Table, string Predicate)[]
{
    ("catalog", "Books", "\"Id\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("catalog", "Editions", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("catalog", "BookAuthors", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("catalog", "BookCategories", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("catalog", "ExternalReferences", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates) OR \"EditionId\" IN (SELECT \"Id\" FROM catalog.\"Editions\" WHERE \"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates))"),
    ("reading", "LibraryEntries", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("profiles", "BedsideBooks", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("profiles", "InfluentialBooks", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)"),
    ("community", "BookRatings", "\"BookId\" IN (SELECT \"Id\" FROM cleanup_candidates)")
};
if (apply)
    await Execute("SET LOCAL lock_timeout = '10s'; LOCK TABLE " +
        string.Join(", ", tables.Select(t => $"{t.Schema}.\"{t.Table}\"")) + " IN SHARE ROW EXCLUSIVE MODE;");
await Execute("""
    CREATE TEMP TABLE cleanup_candidates ON COMMIT DROP AS
    SELECT b."Id", b."Title" FROM catalog."Books" b
    WHERE NOT EXISTS (
        SELECT 1 FROM catalog."Editions" e WHERE e."BookId" = b."Id"
        AND lower(btrim(e."LanguageCode")) IN ('tr', 'tur')
        AND nullif(btrim(e."CoverImageUrl"), '') IS NOT NULL);
    """);
Console.WriteLine($"Mode: {(apply ? "apply" : "report")}; database: {connection.Database}");
Console.WriteLine($"Total books: {await Count("SELECT count(*) FROM catalog.\"Books\"")}");
Console.WriteLine($"Candidates: {await Count("SELECT count(*) FROM cleanup_candidates")}");
foreach (var t in tables)
    Console.WriteLine($"{t.Schema}.{t.Table}: {await Count($"SELECT count(*) FROM {t.Schema}.\"{t.Table}\" WHERE {t.Predicate}")}");
if (!apply || await Count("SELECT count(*) FROM cleanup_candidates") == 0)
{
    await transaction.RollbackAsync();
    return;
}
// Keep an exact database-local recovery snapshot, including linked user records.
var archive = "catalog_cleanup_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
await Execute($"CREATE SCHEMA {archive}");
foreach (var t in tables)
    await Execute($"CREATE TABLE {archive}.\"{t.Schema}_{t.Table}\" AS SELECT * FROM {t.Schema}.\"{t.Table}\" WHERE {t.Predicate}");
// Delete cross-module references explicitly; catalog-owned children cascade.
foreach (var t in tables.Where(t => t.Schema != "catalog"))
    await Execute($"DELETE FROM {t.Schema}.\"{t.Table}\" WHERE {t.Predicate}");
await Execute("DELETE FROM catalog.\"Books\" WHERE \"Id\" IN (SELECT \"Id\" FROM cleanup_candidates)");
if (await Count("SELECT count(*) FROM catalog.\"Books\" WHERE \"Id\" IN (SELECT \"Id\" FROM cleanup_candidates)") != 0)
    throw new InvalidOperationException("Cleanup verification failed; transaction will roll back.");
await transaction.CommitAsync();
Console.WriteLine($"Committed. Recovery schema: {archive}");
