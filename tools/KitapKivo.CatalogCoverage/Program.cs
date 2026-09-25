using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;

if (args.Length != 1) throw new ArgumentException("Usage: CatalogCoverage <new-output.json>");
if (File.Exists(args[0])) throw new ArgumentException("Report already exists.");
var config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load("KitapKivo.Api")).AddEnvironmentVariables().Build();
await using var connection = new NpgsqlConnection(config.GetConnectionString("Database"));
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();
await new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction).ExecuteNonQueryAsync();
await using var command = new NpgsqlCommand("""
SELECT b."Id", b."Title",
 ARRAY(SELECT e."Title" FROM catalog."Editions" e WHERE e."BookId"=b."Id"),
 ARRAY(SELECT a."Name" FROM catalog."BookAuthors" ba JOIN catalog."Authors" a ON a."Id"=ba."AuthorId" WHERE ba."BookId"=b."Id"),
 EXISTS(SELECT 1 FROM catalog."Editions" e WHERE e."BookId"=b."Id" AND e."LanguageCode" IN ('tur','tr') AND NULLIF(btrim(e."CoverImageUrl"),'') IS NOT NULL),
 (SELECT r."Decision"::text FROM catalog."NovelReviews" r WHERE r."BookId"=b."Id" ORDER BY r."RecordedAtUtc" DESC, r."Id" DESC LIMIT 1),
 ARRAY(SELECT r."ExternalId" FROM catalog."ExternalReferences" r WHERE r."BookId"=b."Id" AND r."Source"='OpenLibrary')
FROM catalog."Books" b ORDER BY b."Title"
""", connection, transaction);
var rows = new List<object>();
await using (var reader = await command.ExecuteReaderAsync())
while (await reader.ReadAsync()) rows.Add(new { BookId=reader.GetGuid(0), Title=reader.GetString(1), Editions=reader.GetFieldValue<string[]>(2), Authors=reader.GetFieldValue<string[]>(3), HasCoveredTurkishEdition=reader.GetBoolean(4), Decision=reader.IsDBNull(5)?null:reader.GetString(5), WorkIds=reader.GetFieldValue<string[]>(6) });
await transaction.CommitAsync();
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented=true }));
Console.WriteLine($"Catalog records: {rows.Count}. Report: {args[0]}");

