using System.Diagnostics;
using KitapKivo.Modules.Catalog.Application.Books.Browse;
using KitapKivo.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class CatalogScaleTests(KitapKivoWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    [Fact]
    public async Task HundredThousandBooks_ReturnOnlyRequestedPageAndPreserveJoinedMetadata()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Synthetic records stay inside the disposable Testcontainers database and are rolled back.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO catalog."Books" ("Id", "Title", "OriginalLanguageCode", "CreatedAtUtc")
            SELECT gen_random_uuid(), 'Scale ' || lpad(i::text, 6, '0'), 'tur', '2026-01-01'::timestamptz
            FROM generate_series(1, 100000) i;
            INSERT INTO catalog."Editions" ("Id", "BookId", "Title", "LanguageCode", "Format", "CreatedAtUtc", "CoverImageUrl")
            SELECT gen_random_uuid(), "Id", "Title", 'tur', 'Paperback', "CreatedAtUtc", 'https://covers.openlibrary.org/b/id/123-L.jpg'
            FROM catalog."Books" WHERE "Title" LIKE 'Scale %';
            INSERT INTO catalog."Authors" ("Id", "Name", "CreatedAtUtc")
            SELECT gen_random_uuid(), 'Scale Author ' || i, now() FROM generate_series(1, 3) i;
            INSERT INTO catalog."Categories" ("Id", "Name", "Slug", "CreatedAtUtc")
            SELECT gen_random_uuid(), 'Scale Category ' || i, 'scale-category-' || i, now() FROM generate_series(1, 2) i;
            INSERT INTO catalog."BookAuthors" ("BookId", "AuthorId", "DisplayOrder")
            SELECT b."Id", a."Id", row_number() OVER (PARTITION BY b."Id" ORDER BY a."Name")::int
            FROM catalog."Books" b CROSS JOIN catalog."Authors" a WHERE b."Title" LIKE 'Scale %' AND a."Name" LIKE 'Scale Author %';
            INSERT INTO catalog."BookCategories" ("BookId", "CategoryId")
            SELECT b."Id", c."Id" FROM catalog."Books" b CROSS JOIN catalog."Categories" c
            WHERE b."Title" LIKE 'Scale %' AND c."Slug" LIKE 'scale-category-%';
            ANALYZE catalog."Books";
            ANALYZE catalog."Editions";
            ANALYZE catalog."BookAuthors";
            ANALYZE catalog."BookCategories";
            """);
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();
        foreach (var sort in new[] { CatalogBookSort.RecentlyAdded, CatalogBookSort.Title })
        {
            var query = new BrowseBooksQuery(null, CatalogBookSearchField.All, null, 1, 12, sort);
            await handler.HandleAsync(query); // Warm query compilation and buffers; not a load/SLA assertion.
            var watch = Stopwatch.StartNew();
            var first = await handler.HandleAsync(query);
            output.WriteLine($"100k books, {sort}, warm first page: {watch.ElapsedMilliseconds} ms");
            Assert.Equal(100000, first.TotalCount);
            Assert.Equal(12, first.Items.Count);
            Assert.All(first.Items, book => { Assert.Equal(3, book.Authors.Count); Assert.Equal(2, book.Categories.Count); Assert.NotNull(book.CoverImageUrl); });
            var second = await handler.HandleAsync(new(null, CatalogBookSearchField.All, null, 2, 12, sort));
            Assert.Empty(first.Items.Select(b => b.BookId).Intersect(second.Items.Select(b => b.BookId)));
            Assert.Equal(first.Items.Select(b => b.BookId), (await handler.HandleAsync(query)).Items.Select(b => b.BookId));
        }
        var filtered = await handler.HandleAsync(new("Scale 099999", CatalogBookSearchField.Title, "scale-category-1", 1, 12, CatalogBookSort.Title));
        Assert.Single(filtered.Items);
        await transaction.RollbackAsync();
    }
}
