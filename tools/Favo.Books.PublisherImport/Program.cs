using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

if (args.Length is <4 or >5 || (args.Length==5 && args[4]!="--apply"))
    throw new ArgumentException("Usage: PublisherImport <manifest.json> <target> <batch-id> <result.json> [--apply]");
var bytes=await File.ReadAllBytesAsync(args[0]);
var entries=JsonSerializer.Deserialize<PublisherNovel[]>(bytes) ?? throw new ArgumentException("Invalid manifest.");
var manifestHash=Convert.ToHexString(SHA256.HashData(bytes));
if (args.Length==5)
{
    // A raw collector output is deliberately insufficient for a writing run.
    var approvalPath=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!,"verification-summary.json");
    using var approval=JsonDocument.Parse(await File.ReadAllTextAsync(approvalPath));
    if (!string.Equals(approval.RootElement.GetProperty("ManifestSha256").GetString(),manifestHash,StringComparison.OrdinalIgnoreCase)
        || approval.RootElement.GetProperty("Accepted").GetInt32()!=entries.Length
        || approval.RootElement.GetProperty("CheckedAtUtc").GetDateTimeOffset()<DateTimeOffset.UtcNow.AddDays(-1))
        throw new InvalidOperationException("Current source/cover verification for this exact manifest is required.");
}
var config=new ConfigurationBuilder().AddUserSecrets(Assembly.Load("Favo.Books.Api")).AddEnvironmentVariables().Build();
await using var db=new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(config.GetConnectionString("Database") ?? throw new InvalidOperationException("Missing database configuration.")).Options);
var result=await new ImportPublisherNovels(db).ExecuteAsync(entries,int.Parse(args[1]),args[2],args.Length==5);
var (totalBooks,eligibleBooks)=await new ImportPublisherNovels(db).GetCountsAsync();
var report=new { ManifestSha256=manifestHash, RecordedAtUtc=DateTimeOffset.UtcNow, TotalBooks=totalBooks, EligibleBooks=eligibleBooks, Result=result };
await File.WriteAllTextAsync(args[3],JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
Console.WriteLine(JsonSerializer.Serialize(new { result.Applied,result.AvailableNew,result.ExistingOrDuplicate,Selected=result.Items.Count,TotalBooks=totalBooks,EligibleBooks=eligibleBooks,Report=args[3] }));
