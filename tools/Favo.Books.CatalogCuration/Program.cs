using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--apply"))
    throw new ArgumentException("Usage: CatalogCuration <reviewed-batch.json> [--apply]");
var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
var batch = JsonSerializer.Deserialize<NovelReviewBatch>(await File.ReadAllTextAsync(args[0]), options)
    ?? throw new ArgumentException("Invalid batch.");
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("Favo.Books.Api")).AddEnvironmentVariables().Build();
await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(config.GetConnectionString("Database") ?? throw new InvalidOperationException("Missing database configuration.")).Options);
Console.WriteLine(JsonSerializer.Serialize(await new ReviewNovelBatch(db).ExecuteAsync(batch, args.Length == 2), options));
