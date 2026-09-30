using Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application.Books;

namespace Favo.Books.Api.IntegrationTests;

// Only legacy persistence/worker tests use this source. Safety tests use the production reviewed list.
internal interface IFixtureNovelDetails
{
    ExternalBookDetails? Fixture(string id);
}

internal sealed class FixtureNovelCatalog(IFixtureNovelDetails details, Func<string, string>? title = null) : IApprovedNovelCatalog
{
    public IReadOnlyList<ApprovedNovel> Items => [];
    public ApprovedNovel? Find(string id)
    {
        var book = details.Fixture(id);
        return new(id, book?.Title ?? "Missing approved fixture", title?.Invoke(id) ?? "Türkçe Test Baskısı",
            book?.Authors.Select(a => a.Name).ToArray() ?? ["Test Author"],
            "https://example.com/reviewed-test-fixture", "Isolated test fixture approval", 1);
    }
}
