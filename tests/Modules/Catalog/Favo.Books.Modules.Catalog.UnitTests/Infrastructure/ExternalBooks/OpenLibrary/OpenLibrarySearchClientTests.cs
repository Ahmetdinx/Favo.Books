using System.Net;
using System.Text;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

namespace Favo.Books.Modules.Catalog.UnitTests.Infrastructure
    .ExternalBooks.OpenLibrary;

public sealed class OpenLibrarySearchClientTests
{
    [Fact]
    public async Task SearchAsync_ShouldMapValidDocuments()
    {
        // Arrange
        const string responseJson = """
            {
              "numFound": 2,
              "start": 10,
              "docs": [
                {
                  "key": " /works/OL45804W ",
                  "title": " The Lord of the Rings ",
                  "author_name": [
                    " J. R. R. Tolkien ",
                    " Christopher Tolkien "
                  ],
                  "author_key": [
                    " OL26320A "
                  ],
                  "first_publish_year": 1954,
                  "cover_i": 14625765,
                  "isbn": [
                    "9780618640157",
                    " 9780618640157 ",
                    ""
                  ],
                  "language": [
                    "eng",
                    " ENG ",
                    "tur",
                    ""
                  ]
                },
                {
                  "key": " ",
                  "title": "Invalid document"
                }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler(
            CreateJsonResponse(responseJson));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };

        var client = new OpenLibrarySearchClient(httpClient);

        // Act
        ExternalBookSearchResult result =
            await client.SearchAsync(
                "lord rings",
                page: 2,
                pageSize: 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);

        ExternalBookSearchItem item =
            Assert.Single(result.Items);

        Assert.Equal("OpenLibrary", item.Provider);
        Assert.Equal("/works/OL45804W", item.ExternalId);
        Assert.Equal("The Lord of the Rings", item.Title);
        Assert.Equal(1954, item.FirstPublishedYear);
        Assert.Equal(
            "https://covers.openlibrary.org/b/id/14625765-M.jpg",
            item.CoverUrl);

        Assert.Collection(
            item.Authors,
            author =>
            {
                Assert.Equal("OL26320A", author.ExternalId);
                Assert.Equal("J. R. R. Tolkien", author.Name);
            },
            author =>
            {
                Assert.Null(author.ExternalId);
                Assert.Equal("Christopher Tolkien", author.Name);
            });

        Assert.Equal(
            new[] { "9780618640157" },
            item.Isbns);

        Assert.Equal(
            new[] { "eng", "tur" },
            item.LanguageCodes);

        HttpRequestMessage request =
            Assert.IsType<HttpRequestMessage>(handler.Request);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.NotNull(request.RequestUri);
        Assert.Equal(
            "/search.json",
            request.RequestUri.AbsolutePath);
        Assert.Contains(
            "q=lord%20rings",
            request.RequestUri.Query);
        Assert.Contains("page=2", request.RequestUri.Query);
        Assert.Contains("limit=10", request.RequestUri.Query);
        Assert.Contains("fields=", request.RequestUri.Query);
    }

    [Fact]
    public async Task SearchAsync_ShouldThrowJsonException_WhenBodyIsNull()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            CreateJsonResponse("null"));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };

        var client = new OpenLibrarySearchClient(httpClient);

        // Act
        JsonException exception =
            await Assert.ThrowsAsync<JsonException>(() =>
                client.SearchAsync(
                    "dune",
                    page: 1,
                    pageSize: 20));

        // Assert
        Assert.Contains(
            "empty search response",
            exception.Message);
    }

    private static HttpResponseMessage CreateJsonResponse(
        string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;

            return Task.FromResult(response);
        }
    }
}