using System.Net;
using System.Text;

using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

namespace KitapKivo.Modules.Catalog.UnitTests
    .Infrastructure.ExternalBooks.OpenLibrary;

public sealed class OpenLibraryBookDetailsClientTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldMapWorkAndSearchData()
    {
        // Arrange
        const string json = """
            {
              "key": "/works/OL45804W",
              "title": "  Dune  ",
              "description": {
                "value": "  Epic science fiction novel.  "
              },
              "first_publish_date": "June 1965",
              "covers": [-1, 12345],
              "authors": [
                {
                  "author": {
                    "key": "/authors/OL60038A"
                  }
                },
                {
                  "author": {
                    "key": "/authors/OL99999A"
                  }
                }
              ],
              "subjects": [
                " Science Fiction ",
                "science fiction",
                "",
                "Adventure"
              ]
            }
            """;

        const string authorJson = """
            {
              "key": "/authors/OL60038A",
              "name": "  Frank Herbert  ",
              "alternate_names": [
                "Herbert, Frank",
                "Фрэнк Герберт",
                "frank herbert"
              ]
            }
            """;

        const string duplicateAuthorJson = """
            {
              "key": "/authors/OL99999A",
              "name": "  Френк Герберт  "
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/works/OL45804W.json" =>
                        CreateJsonResponse(json),

                    "/authors/OL60038A.json" =>
                        CreateJsonResponse(authorJson),

                    "/authors/OL99999A.json" =>
                        CreateJsonResponse(
                            duplicateAuthorJson),

                    _ => new HttpResponseMessage(
                        HttpStatusCode.NotFound)
                });

        var searchResult = new ExternalBookSearchResult(
            1,
            1,
            1,
            [
                new ExternalBookSearchItem(
                    "OpenLibrary",
                    "/works/OL45804W",
                    "Dune",
                    [
                        new ExternalBookAuthor(
                            "/authors/OL60038A",
                            "Frank Herbert"),
                        new ExternalBookAuthor(
                            "/authors/OL99999A",
                            "Френк Герберт")
                    ],
                    null,
                    "https://example.com/fallback.jpg",
                    ["9780441013593"],
                    ["eng"])
            ]);

        var searchClient =
            new StubExternalBookSearchClient(searchResult);

        using var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client = new OpenLibraryBookDetailsClient(
            httpClient,
            searchClient);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act
        ExternalBookDetails details =
            Assert.IsType<ExternalBookDetails>(
                await client.GetByIdAsync(
                    "/works/OL45804W",
                    cancellationTokenSource.Token));

        // Assert
        Assert.Equal(
            "OpenLibrary",
            details.Provider);

        Assert.Equal(
            "/works/OL45804W",
            details.ExternalId);

        Assert.Equal(
            "Dune",
            details.Title);

        Assert.Equal(
            "Epic science fiction novel.",
            details.Description);

        Assert.Equal(
            1965,
            details.FirstPublishedYear);

        Assert.Equal(
            "https://covers.openlibrary.org/b/id/12345-L.jpg",
            details.CoverUrl);

        ExternalBookAuthor author =
            Assert.Single(details.Authors);

        Assert.Equal(
            "/authors/OL60038A",
            author.ExternalId);

        Assert.Equal(
            "Frank Herbert",
            author.Name);

        Assert.Equal(
            "9780441013593",
            Assert.Single(details.Isbns));

        Assert.Equal(
            "eng",
            Assert.Single(details.LanguageCodes));

        Assert.Collection(
            details.Subjects,
            subject => Assert.Equal(
                "Science Fiction",
                subject),
            subject => Assert.Equal(
                "Adventure",
                subject));

        Assert.Equal(
            3,
            httpHandler.CallCount);

        Assert.All(
            httpHandler.RequestUris,
            requestUri => Assert.NotNull(
                requestUri));

        Assert.Equal(
            new Uri(
                "https://openlibrary.org/" +
                "works/OL45804W.json"),
            httpHandler.RequestUris[0]);

        Assert.Equal(
            new Uri(
                "https://openlibrary.org/" +
                "authors/OL60038A.json"),
            httpHandler.RequestUris[1]);

        Assert.Equal(
            new Uri(
                "https://openlibrary.org/" +
                "authors/OL99999A.json"),
            httpHandler.RequestUris[2]);

        Assert.Equal(
            "key:/works/OL45804W",
            searchClient.Query);

        Assert.Equal(
            1,
            searchClient.Page);

        Assert.Equal(
            1,
            searchClient.PageSize);

        Assert.Equal(
            cancellationTokenSource.Token,
            searchClient.CancellationToken);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMapStringDescription()
    {
        // Arrange
        const string json = """
            {
              "key": "/works/OL123W",
              "title": "Example Book",
              "description": "  Plain description.  ",
              "first_publish_date": "2001-04-15",
              "covers": [],
              "subjects": []
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(json));

        var searchClient =
            new StubExternalBookSearchClient(
                CreateEmptySearchResult());

        using var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client = new OpenLibraryBookDetailsClient(
            httpClient,
            searchClient);

        // Act
        ExternalBookDetails details =
            Assert.IsType<ExternalBookDetails>(
                await client.GetByIdAsync(
                    "works/OL123W"));

        // Assert
        Assert.Equal(
            "/works/OL123W",
            details.ExternalId);

        Assert.Equal(
            "Plain description.",
            details.Description);

        Assert.Equal(
            2001,
            details.FirstPublishedYear);

        Assert.Null(
            details.CoverUrl);
    }

    [Fact]
    public async Task
        GetByIdAsync_ShouldUseSearchData_WhenWorkDataIsMissing()
    {
        // Arrange
        const string json = """
            {
              "key": "/works/OL456W",
              "title": " ",
              "covers": [],
              "subjects": []
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(json));

        var searchResult = new ExternalBookSearchResult(
            1,
            1,
            1,
            [
                new ExternalBookSearchItem(
                    "OpenLibrary",
                    "/works/OL456W",
                    "Fallback Title",
                    [],
                    1977,
                    "https://example.com/cover.jpg",
                    [],
                    [])
            ]);

        var searchClient =
            new StubExternalBookSearchClient(searchResult);

        using var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client = new OpenLibraryBookDetailsClient(
            httpClient,
            searchClient);

        // Act
        ExternalBookDetails details =
            Assert.IsType<ExternalBookDetails>(
                await client.GetByIdAsync(
                    "/works/OL456W"));

        // Assert
        Assert.Equal(
            "Fallback Title",
            details.Title);

        Assert.Equal(
            1977,
            details.FirstPublishedYear);

        Assert.Equal(
            "https://example.com/cover.jpg",
            details.CoverUrl);
    }

    [Fact]
    public async Task
        GetByIdAsync_ShouldReturnNull_WhenWorkDoesNotExist()
    {
        // Arrange
        var httpHandler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(
                HttpStatusCode.NotFound));

        var searchClient =
            new StubExternalBookSearchClient(
                CreateEmptySearchResult());

        using var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client = new OpenLibraryBookDetailsClient(
            httpClient,
            searchClient);

        // Act
        ExternalBookDetails? details =
            await client.GetByIdAsync(
                "/works/OL404W");

        // Assert
        Assert.Null(
            details);

        Assert.Equal(
            0,
            searchClient.CallCount);
    }

    [Fact]
    public async Task
        GetByIdAsync_ShouldThrow_WhenIdIsNotWorkId()
    {
        // Arrange
        var httpHandler = new StubHttpMessageHandler(
            _ => throw new InvalidOperationException(
                "HTTP request should not be sent."));

        var searchClient =
            new StubExternalBookSearchClient(
                CreateEmptySearchResult());

        using var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client = new OpenLibraryBookDetailsClient(
            httpClient,
            searchClient);

        // Act and assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.GetByIdAsync(
                "/authors/OL123A"));

        Assert.Equal(
            0,
            httpHandler.CallCount);

        Assert.Equal(
            0,
            searchClient.CallCount);
    }

    private static ExternalBookSearchResult
        CreateEmptySearchResult()
    {
        return new ExternalBookSearchResult(
            0,
            1,
            1,
            []);
    }

    private static HttpResponseMessage CreateJsonResponse(
        string json)
    {
        return new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage>
            responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public List<Uri?> RequestUris { get; } = [];

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            RequestUris.Add(request.RequestUri);

            return Task.FromResult(
                responseFactory(request));
        }
    }

    private sealed class StubExternalBookSearchClient(
        ExternalBookSearchResult result)
        : IExternalBookSearchClient
    {
        public int CallCount { get; private set; }

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
            CallCount++;
            Query = query;
            Page = page;
            PageSize = pageSize;
            CancellationToken = cancellationToken;

            return Task.FromResult(result);
        }
    }
}