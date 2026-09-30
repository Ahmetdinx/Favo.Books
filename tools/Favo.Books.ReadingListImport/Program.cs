using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Application.Books.Browse;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

if (args.Length is < 3 or > 4 || (args.Length == 4 && args[3] != "--apply"))
    throw new ArgumentException("Usage: ReadingListImport <manifest.json> <batch-id> <new-result.json> [--apply]");
if (File.Exists(args[2])) throw new ArgumentException("Result already exists.");
var bytes = await File.ReadAllBytesAsync(args[0]);
var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
var entries = JsonSerializer.Deserialize<ReviewedReadingListEntry[]>(bytes, options)!;
if (args.Length == 4)
{
    using var verification = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "verification.json")));
    var v = verification.RootElement;
    if (!string.Equals(v.GetProperty("ManifestSha256").GetString(), Convert.ToHexString(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase)
        || v.GetProperty("Verified").GetInt32() != entries.Length
        || v.GetProperty("CheckedAtUtc").GetDateTimeOffset() < DateTimeOffset.UtcNow.AddDays(-1))
        throw new InvalidOperationException("Fresh exact-manifest source and cover verification is required.");
}
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("Favo.Books.Api")).AddEnvironmentVariables().Build();
await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(config.GetConnectionString("Database")).Options);
var result = await new ImportReviewedReadingList(db).ExecuteAsync(entries, args[1], args.Length == 4);
await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(result, options));
if (args.Length == 4)
{
    var checks = new List<object>();
    foreach (var item in result)
    {
        var page = await new BrowseBooksHandler(db).HandleAsync(new(item.Title,
            CatalogBookSearchField.Title, null, 1, 1000, CatalogBookSort.Title));
        var found = page.Items.Any(b => b.BookId == item.BookId);
        checks.Add(new { item.Number, item.BookId, item.Title, FoundInCatalogSearch = found });
        if (!found) throw new InvalidOperationException($"Catalog search verification failed for #{item.Number}.");
    }
    await File.WriteAllTextAsync(args[2] + ".catalog-check.json", JsonSerializer.Serialize(checks, options));
}
Console.WriteLine(JsonSerializer.Serialize(new { Count = result.Length, Created = result.Count(x => x.Created),
    Visible = result.Count(x => x.Visible), Applied = args.Length == 4, Report = args[2] }));
