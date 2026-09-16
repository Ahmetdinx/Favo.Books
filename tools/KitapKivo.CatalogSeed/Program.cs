using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application.Books.ResolveExternal;
using KitapKivo.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

if (args.Length != 2 || !int.TryParse(args[0], out var target) || target is < 1 or > 5000)
    throw new ArgumentException("Usage: CatalogSeed <target-new-books:1..5000> <run-directory>");
var runDirectory = Path.GetFullPath(args[1]);
Directory.CreateDirectory(runDirectory);
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("KitapKivo.Api"))
    .AddEnvironmentVariables().Build();
var connectionString = config.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Database configuration is missing.");
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
await using (var gate = new NpgsqlCommand("SELECT pg_try_advisory_lock(842619001)", connection))
    if (!(bool)(await gate.ExecuteScalarAsync())!) throw new InvalidOperationException("Another catalog seed is running.");
async Task<HashSet<string>> LoadStrings(string sql)
{
    await using var command = new NpgsqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync()) if (!reader.IsDBNull(0)) values.Add(reader.GetString(0));
    return values;
}
var knownReferences = await LoadStrings("SELECT \"ExternalId\" FROM catalog.\"ExternalReferences\" WHERE \"Source\" = 'OpenLibrary'");
var knownIsbns = await LoadStrings("SELECT \"Isbn13\" FROM catalog.\"Editions\" UNION SELECT \"Isbn10\" FROM catalog.\"Editions\"");
var signatures = await LoadStrings("""
    SELECT upper(b."Title") || '|' || upper(a."Name") FROM catalog."Books" b
    JOIN catalog."BookAuthors" ba ON ba."BookId" = b."Id"
    JOIN catalog."Authors" a ON a."Id" = ba."AuthorId"
    """);
var manifestPath = Path.Combine(runDirectory, "created.jsonl");
var created = File.Exists(manifestPath) ? File.ReadLines(manifestPath).Count(line => !string.IsNullOrWhiteSpace(line)) : 0;
var skipped = 0;
var failures = 0;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("KitapKivo/1.0 (Turkish-edition catalog import)");
Console.WriteLine($"Target new books: {target}; already recorded in this run: {created}");
for (var page = 1; page <= 150 && created < target; page++)
{
    var cache = Path.Combine(runDirectory, $"search-{page:D3}.json");
    string json;
    if (File.Exists(cache)) json = await File.ReadAllTextAsync(cache);
    else
    {
        const string fields = "key,title,author_key,author_name,first_publish_year,subject,editions,editions.key,editions.title,editions.language,editions.cover_i,editions.isbn,editions.publisher,editions.publish_date";
        var uri = $"https://openlibrary.org/search.json?q=language%3Atur&lang=tr&limit=100&page={page}&fields={fields}";
        json = await FetchAsync(uri);
        await File.WriteAllTextAsync(cache, json);
    }
    using var document = JsonDocument.Parse(json);
    var docs = document.RootElement.GetProperty("docs");
    if (docs.GetArrayLength() == 0) break;
    foreach (var work in docs.EnumerateArray())
    {
        if (created >= target) break;
        var workId = Text(work, "key");
        if (workId is null || !Regex.IsMatch(workId, @"^/works/OL\d+W$") || knownReferences.Contains(workId)) { skipped++; continue; }
        if (!work.TryGetProperty("editions", out var editions) ||
            !editions.TryGetProperty("docs", out var editionDocs) || editionDocs.GetArrayLength() == 0) { skipped++; continue; }
        var edition = editionDocs[0];
        var editionId = Text(edition, "key");
        var title = Text(edition, "title");
        var cover = Number(edition, "cover_i");
        var languages = Strings(edition, "language");
        var names = Strings(work, "author_name");
        var authorKeys = Strings(work, "author_key");
        if (editionId is null || !Regex.IsMatch(editionId, @"^/books/OL\d+M$") ||
            title is null || title.Length > 500 || cover is not > 0 || names.Length == 0 ||
            !languages.Any(language => language is "tur" or "tr") || knownReferences.Contains(editionId)) { skipped++; continue; }
        var isbns = Strings(edition, "isbn").Select(isbn => isbn.Replace("-", "").Replace(" ", "").ToUpperInvariant()).ToArray();
        if (isbns.Any(knownIsbns.Contains) || names.Any(name => signatures.Contains(title.ToUpperInvariant() + "|" + name.ToUpperInvariant()))) { skipped++; continue; }
        var authors = names.Select((name, index) => new ExternalBookAuthor(
            index < authorKeys.Length && Regex.IsMatch(authorKeys[index], @"^OL\d+A$") ? "/authors/" + authorKeys[index] : null, name)).ToArray();
        var workTitle = Text(work, "title") ?? title;
        if (workTitle.Length > 500) { skipped++; continue; }
        var details = new ExternalBookDetails("OpenLibrary", workId, workTitle, null, authors,
            Number(work, "first_publish_year") is >= 0 and <= 9999 ? Number(work, "first_publish_year") : null,
            null, isbns, [], Strings(work, "subject"));
        var yearText = Strings(edition, "publish_date").FirstOrDefault();
        var yearMatch = Regex.Match(yearText ?? "", @"(?<!\d)\d{4}(?!\d)");
        int? year = yearMatch.Success ? int.Parse(yearMatch.Value) : null;
        var editionDetails = new ExternalBookEditionDetails("OpenLibrary", editionId, title, "tur", null,
            isbns.FirstOrDefault(isbn => Regex.IsMatch(isbn, @"^\d{9}[\dX]$")),
            isbns.FirstOrDefault(isbn => Regex.IsMatch(isbn, @"^\d{13}$")), null, year,
            $"https://covers.openlibrary.org/b/id/{cover}-L.jpg", Strings(edition, "publisher").FirstOrDefault());
        try
        {
            await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
            var source = new SearchRecordSource(details, editionDetails);
            var handler = new ResolveExternalBookHandler(db, source, source);
            var result = await handler.HandleAsync(new ResolveExternalBookCommand("OpenLibrary", workId));
            if (result.IsCreated)
            {
                // Append each successful book immediately; source pages make the run auditable.
                await File.AppendAllTextAsync(manifestPath, JsonSerializer.Serialize(new { result.BookId, WorkId = workId, EditionId = editionId, Title = title, Page = page }) + Environment.NewLine);
                created++;
                if (created % 50 == 0) Console.WriteLine($"Created {created}/{target}; page {page}; skipped {skipped}; failures {failures}");
            }
            else skipped++;
            knownReferences.Add(workId);
            knownReferences.Add(editionId);
            foreach (var isbn in isbns) knownIsbns.Add(isbn);
            foreach (var name in names) signatures.Add(title.ToUpperInvariant() + "|" + name.ToUpperInvariant());
        }
        catch (Exception exception) when (exception is ArgumentException or DbUpdateException)
        {
            failures++;
            await File.AppendAllTextAsync(Path.Combine(runDirectory, "failures.jsonl"),
                JsonSerializer.Serialize(new { WorkId = workId, Error = exception.GetType().Name, Reason = exception.InnerException is PostgresException pg ? pg.SqlState : null }) + Environment.NewLine);
        }
    }
    Console.WriteLine($"Page {page} complete: created {created}/{target}; skipped {skipped}; failures {failures}");
}
Console.WriteLine($"Finished: {created} new books recorded. Manifest: {manifestPath}");
if (created < target) Environment.ExitCode = 2;

async Task<string> FetchAsync(string uri)
{
    for (var attempt = 0; ; attempt++)
    {
        // Sequential requests, at most one per second, with bounded retry/backoff.
        await Task.Delay(TimeSpan.FromSeconds(attempt == 0 ? 1 : Math.Min(30, 2 << attempt)));
        try
        {
            using var response = await http.GetAsync(uri);
            if ((response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < 4) continue;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception error) when (attempt < 4 && error is HttpRequestException or TaskCanceledException) { }
    }
}
static string? Text(JsonElement item, string field) => item.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
static int? Number(JsonElement item, string field) => item.TryGetProperty(field, out var value) && value.TryGetInt32(out var number) ? number : null;
static string[] Strings(JsonElement item, string field) => item.TryGetProperty(field, out var values) && values.ValueKind == JsonValueKind.Array
    ? values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!.Trim()).Where(value => value.Length > 0).ToArray() : [];

sealed class SearchRecordSource(ExternalBookDetails work, ExternalBookEditionDetails edition) : IExternalBookDetailsClient, IExternalBookEditionClient
{
    public Task<ExternalBookDetails?> GetByIdAsync(string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ExternalBookDetails?>(externalId == work.ExternalId ? work : null);
    public Task<ExternalBookEditionDetails?> GetBestForWorkAsync(string externalWorkId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ExternalBookEditionDetails?>(externalWorkId == work.ExternalId ? edition : null);
}
