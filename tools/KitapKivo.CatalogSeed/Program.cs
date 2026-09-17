using System.Reflection;
using System.Text.Json;
using KitapKivo.Modules.Catalog;
using KitapKivo.Modules.Catalog.Application.Books;
using KitapKivo.Modules.Catalog.Application.Books.ResolveExternal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

if (args.Length is < 2 or > 3 || !int.TryParse(args[0], out var target) || target is < 1 or > 10000
    || (args.Length == 3 && args[2] != "--apply"))
    throw new ArgumentException("Usage: CatalogSeed <maximum-new-books:1..10000> <new-run-directory> [--apply]");
var list = ApprovedNovelCatalog.Default.Items;
if (args.Length == 2)
{
    Console.WriteLine(JsonSerializer.Serialize(new { Mode = "Preview", MaximumNewBooks = target,
        AvailableReviewedWorks = list.Count, Items = list.Select(x => new { x.WorkId, x.TurkishTitle, x.Priority }) }));
    return;
}
var directory = Path.GetFullPath(args[1]);
if (Directory.Exists(directory)) throw new ArgumentException("Use a new directory; legacy broad imports cannot be resumed.");
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("KitapKivo.Api"))
    .AddEnvironmentVariables().Build();
await using var gateConnection = new NpgsqlConnection(config.GetConnectionString("Database"));
await gateConnection.OpenAsync();
await using (var gate = new NpgsqlCommand("SELECT pg_try_advisory_lock(842619001)", gateConnection))
    if (!(bool)(await gate.ExecuteScalarAsync())!) throw new InvalidOperationException("Another catalog seed is running.");
var services = new ServiceCollection();
services.AddLogging();
services.AddCatalogModule(config);
await using var provider = services.BuildServiceProvider();
Directory.CreateDirectory(directory);
await File.WriteAllTextAsync(Path.Combine(directory, "policy.json"), JsonSerializer.Serialize(new
{
    Policy = "reviewed-classics-v1", CreatedAtUtc = DateTimeOffset.UtcNow, MaximumNewBooks = target,
    ReviewedWorks = list
}));
int created = 0, existing = 0, rejected = 0;
foreach (var candidate in list)
{
    if (created >= target) break;
    await Task.Delay(TimeSpan.FromSeconds(1));
    await using var scope = provider.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<ResolveExternalBookHandler>()
        .HandleAsync(new("OpenLibrary", candidate.WorkId));
    if (result.IsCreated) created++;
    else if (result.IsSuccess) existing++;
    else rejected++;
    await File.AppendAllTextAsync(Path.Combine(directory, "results.jsonl"),
        JsonSerializer.Serialize(new { candidate.WorkId, result.BookId, result.IsCreated, result.RejectionReason, result.IsSuccess }) + Environment.NewLine);
    Console.WriteLine($"Created: {created}; existing: {existing}; rejected: {rejected}");
}
Console.WriteLine($"Finished reviewed list: {created} created, {existing} existing, {rejected} rejected. No random fallback.");
if (rejected > 0) Environment.ExitCode = 2;
