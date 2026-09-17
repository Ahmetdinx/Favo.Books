using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using KitapKivo.Modules.Catalog.Domain.Books;
using Microsoft.Extensions.Configuration;
using Npgsql;

if (args.Length < 2)
    throw new ArgumentException("Usage: CatalogAudit <new-report-directory> <cached-import-directory> [more-cache-directories...]");
var output = Path.GetFullPath(args[0]);
if (Directory.Exists(output)) throw new ArgumentException("Use a new report directory to preserve previous audits.");
var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
var evidence = new Dictionary<string, List<SourceEvidence>>(StringComparer.Ordinal);
var files = args.Skip(1).Select(Path.GetFullPath).SelectMany(dir => Directory.GetFiles(dir, "search-*.json"))
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
var sourceFiles = new List<object>();
foreach (var file in files)
{
    var bytes = await File.ReadAllBytesAsync(file);
    using var json = JsonDocument.Parse(bytes);
    sourceFiles.Add(new { File = file, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)) });
    foreach (var work in json.RootElement.GetProperty("docs").EnumerateArray())
    {
        if (!work.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String) continue;
        var workId = key.GetString()!;
        var subjects = work.TryGetProperty("subject", out var subject) && subject.ValueKind == JsonValueKind.Array
            ? subject.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!).ToArray() : [];
        if (!evidence.TryGetValue(workId, out var entries)) evidence[workId] = entries = [];
        entries.Add(new(workId, file, subjects));
    }
}
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("KitapKivo.Api")).AddEnvironmentVariables().Build();
await using var connection = new NpgsqlConnection(config.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Database configuration is missing."));
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead);
await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
    await readOnly.ExecuteNonQueryAsync();
await using var command = new NpgsqlCommand("""
    SELECT b."Id", b."Title",
      ARRAY(SELECT r."ExternalId" FROM catalog."ExternalReferences" r
        WHERE r."BookId" = b."Id" AND r."Source" = 'OpenLibrary' ORDER BY r."ExternalId"),
      EXISTS(SELECT 1 FROM catalog."Editions" e WHERE e."BookId" = b."Id"
        AND e."LanguageCode" IN ('tur', 'tr') AND NULLIF(btrim(e."CoverImageUrl"), '') IS NOT NULL)
    FROM catalog."Books" b ORDER BY b."Id"
    """, connection, transaction);
Directory.CreateDirectory(output);
var reports = new List<AuditRow>();
await using (var reader = await command.ExecuteReaderAsync())
await using (var writer = new StreamWriter(Path.Combine(output, "books.jsonl")))
{
    while (await reader.ReadAsync())
    {
        var references = reader.GetFieldValue<string[]>(2);
        var sources = references.SelectMany(id => evidence.GetValueOrDefault(id) ?? []).ToArray();
        var result = NovelEvidenceClassifier.Classify(sources.SelectMany(s => s.Subjects));
        var row = new AuditRow(reader.GetGuid(0), reader.GetString(1), references, reader.GetBoolean(3),
            sources.Length > 0, result, sources);
        reports.Add(row);
        await writer.WriteLineAsync(JsonSerializer.Serialize(row, options));
    }
}
await transaction.CommitAsync();
var summary = new
{
    GeneratedAtUtc = DateTimeOffset.UtcNow,
    RuleVersion = NovelEvidenceClassifier.Version,
    Mode = "Read-only metadata triage; no publication, deletion or image validation",
    TotalBooks = reports.Count,
    SourceFiles = files.Length,
    BooksWithCachedEvidence = reports.Count(r => r.HasCachedEvidence),
    BooksWithoutCachedEvidence = reports.Count(r => !r.HasCachedEvidence),
    MissingCoveredTurkishEdition = reports.Count(r => !r.HasCoveredTurkishEdition),
    StatusCounts = Enum.GetValues<NovelEvidenceStatus>().ToDictionary(s => s.ToString(), s => reports.Count(r => r.Classification.Status == s)),
    ReviewSamples = Enum.GetValues<NovelEvidenceStatus>().ToDictionary(s => s.ToString(),
        s => reports.Where(r => r.Classification.Status == s).Take(10).Select(r => new { r.BookId, r.Title, r.Classification }).ToArray())
};
var pretty = new JsonSerializerOptions(options) { WriteIndented = true };
await File.WriteAllTextAsync(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, pretty));
await File.WriteAllTextAsync(Path.Combine(output, "sources.json"), JsonSerializer.Serialize(sourceFiles, pretty));
Console.WriteLine(JsonSerializer.Serialize(new { summary.TotalBooks, summary.BooksWithCachedEvidence,
    summary.BooksWithoutCachedEvidence, summary.MissingCoveredTurkishEdition, summary.StatusCounts, ReportDirectory = output }, pretty));

sealed record SourceEvidence(string WorkId, string File, string[] Subjects);
sealed record AuditRow(Guid BookId, string Title, string[] WorkIds, bool HasCoveredTurkishEdition,
    bool HasCachedEvidence, NovelEvidenceResult Classification, SourceEvidence[] Sources);
