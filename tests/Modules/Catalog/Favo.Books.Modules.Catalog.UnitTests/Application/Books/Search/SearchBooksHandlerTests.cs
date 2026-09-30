using Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application.Books.Search;

namespace Favo.Books.Modules.Catalog.UnitTests.Application.Books.Search;

public sealed class SearchBooksHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldForwardQueryToSearchClient()
    {
        // Arrange
        var expectedResult = new ExternalBookSearchResult(
            TotalCount: 42,
            Page: 2,
            PageSize: 10,
            Items: []);

        var searchClient = new StubExternalBookSearchClient(
            expectedResult);

        var handler = new SearchBooksHandler(searchClient);

        var query = new SearchBooksQuery(
            Query: "dune",
            Page: 2,
            PageSize: 10);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act
        ExternalBookSearchResult result =
            await handler.HandleAsync(
                query,
                cancellationTokenSource.Token);

        // Assert
        Assert.Same(expectedResult, result);
        Assert.Equal("dune", searchClient.Query);
        Assert.Equal(2, searchClient.Page);
        Assert.Equal(10, searchClient.PageSize);
        Assert.Equal(
            cancellationTokenSource.Token,
            searchClient.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenQueryIsNull()
    {
        // Arrange
        var searchClient = new StubExternalBookSearchClient(
            new ExternalBookSearchResult(
                TotalCount: 0,
                Page: 1,
                PageSize: 20,
                Items: []));

        var handler = new SearchBooksHandler(searchClient);

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!));
    }

    private sealed class StubExternalBookSearchClient(
        ExternalBookSearchResult result)
        : IExternalBookSearchClient
    {
        public string? Query { get; private set; }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<ExternalBookSearchResult> SearchAsync(
            string query,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            Page = page;
            PageSize = pageSize;
            CancellationToken = cancellationToken;

            return Task.FromResult(result);
        }
    }
}