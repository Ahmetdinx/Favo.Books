using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests;

internal static class CatalogTestData
{
    // Reading/Profile/Community tests need existing catalog fixtures, not an unreviewed public write bypass.
    public static async Task<Guid> CreateBookAsync(FavoBooksWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var author = new Author("Test Author");
        var book = new Book("Test Book", "tr");
        book.Authors.Add(new BookAuthor(book.Id, author.Id, 0));
        db.Authors.Add(author);
        db.Books.Add(book);
        db.NovelReviews.Add(new NovelReview(book.Id, NovelDecision.Novel, Guid.NewGuid().ToString("N"),
            "https://example.com/novel", "Reviewed test fixture", "test", "test-v1"));
        await db.SaveChangesAsync();
        return book.Id;
    }
}
