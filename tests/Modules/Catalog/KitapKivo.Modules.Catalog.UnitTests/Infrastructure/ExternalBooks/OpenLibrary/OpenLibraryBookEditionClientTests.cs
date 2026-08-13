using System.Net;
using System.Text;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

namespace KitapKivo.Modules.Catalog.UnitTests
    .Infrastructure.ExternalBooks.OpenLibrary;

public sealed class OpenLibraryBookEditionClientTests
{
    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldMapBestEdition()
    {
        // Arrange
        const string searchJson = """
            {
              "numFound": 1,
              "docs": [
                {
                  "key": "/works/OL45804W",
                  "editions": {
                    "numFound": 2,
                    "docs": [
                      {
                        "key": "/books/OL7353617M"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        const string editionJson = """
            {
              "key": "/books/OL7353617M",
              "title": "  Dune  ",
              "languages": [
                {
                  "key": "/languages/eng"
                }
              ],
              "physical_format": "  Paperback  ",
              "isbn_10": [
                "invalid",
                " 0441172717 "
              ],
              "isbn_13": [
                "invalid",
                "9780441172719"
              ],
              "number_of_pages": 896,
              "publish_date": "August 15, 1999",
              "covers": [-1, 12345],
              "publishers": [
                " ",
                "  Ace  "
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/search.json" =>
                        CreateJsonResponse(searchJson),

                    "/books/OL7353617M.json" =>
                        CreateJsonResponse(editionJson),

                    _ => new HttpResponseMessage(
                        HttpStatusCode.NotFound)
                });

        using var httpClient = new HttpClient(
            httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act
        ExternalBookEditionDetails details =
            Assert.IsType<ExternalBookEditionDetails>(
                await client.GetBestForWorkAsync(
                    "/works/OL45804W",
                    cancellationTokenSource.Token));

        // Assert
        Assert.Equal(
            "OpenLibrary",
            details.Provider);

        Assert.Equal(
            "/books/OL7353617M",
            details.ExternalId);

        Assert.Equal(
            "Dune",
            details.Title);

        Assert.Equal(
            "eng",
            details.LanguageCode);

        Assert.Equal(
            "Paperback",
            details.Format);

        Assert.Equal(
            "0441172717",
            details.Isbn10);

        Assert.Equal(
            "9780441172719",
            details.Isbn13);

        Assert.Equal(
            896,
            details.PageCount);

        Assert.Equal(
            1999,
            details.PublicationYear);

        Assert.Equal(
            "https://covers.openlibrary.org/" +
            "b/id/12345-L.jpg",
            details.CoverUrl);

        Assert.Equal(
            "Ace",
            details.PublisherName);

        Assert.Equal(
            2,
            httpHandler.CallCount);

        Assert.Equal(
            2,
            httpHandler.RequestUris.Count);

        Assert.Equal(
            "/search.json",
            httpHandler.RequestUris[0]
                ?.AbsolutePath);

        string searchQuery =
            Uri.UnescapeDataString(
                httpHandler.RequestUris[0]
                    ?.Query ??
                string.Empty);

        Assert.Contains(
            "q=key:/works/OL45804W",
            searchQuery);

        Assert.Contains(
            "fields=key,editions,editions.key",
            searchQuery);

        Assert.Equal(
            "/books/OL7353617M.json",
            httpHandler.RequestUris[1]
                ?.AbsolutePath);

        Assert.True(
      httpHandler.CancellationToken
          .CanBeCanceled);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldReturnNull_WhenEditionIsMissing()
    {
        // Arrange
        const string searchJson = """
            {
              "numFound": 1,
              "docs": [
                {
                  "key": "/works/OL45804W",
                  "editions": {
                    "numFound": 0,
                    "docs": []
                  }
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(searchJson));

        using var httpClient = new HttpClient(
            httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        // Act
        ExternalBookEditionDetails? details =
            await client.GetBestForWorkAsync(
                "/works/OL45804W");

        // Assert
        Assert.Null(details);

        Assert.Equal(
            1,
            httpHandler.CallCount);

        Assert.Equal(
            "/search.json",
            httpHandler.RequestUris[0]
                ?.AbsolutePath);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldReturnNull_WhenEditionDoesNotExist()
    {
        // Arrange
        const string searchJson = """
            {
              "numFound": 1,
              "docs": [
                {
                  "key": "/works/OL45804W",
                  "editions": {
                    "numFound": 1,
                    "docs": [
                      {
                        "key": "/books/OL7353617M"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/search.json" =>
                        CreateJsonResponse(searchJson),

                    "/books/OL7353617M.json" =>
                        new HttpResponseMessage(
                            HttpStatusCode.NotFound),

                    _ => new HttpResponseMessage(
                        HttpStatusCode.NotFound)
                });

        using var httpClient = new HttpClient(
            httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        // Act
        ExternalBookEditionDetails? details =
            await client.GetBestForWorkAsync(
                "/works/OL45804W");

        // Assert
        Assert.Null(details);

        Assert.Equal(
            2,
            httpHandler.CallCount);

        Assert.Equal(
            "/books/OL7353617M.json",
            httpHandler.RequestUris[1]
                ?.AbsolutePath);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldThrow_WhenWorkIdIsInvalid()
    {
        // Arrange
        var httpHandler = new StubHttpMessageHandler(
            _ => throw new InvalidOperationException(
                "HTTP request should not be sent."));

        using var httpClient = new HttpClient(
            httpHandler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        // Act and assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => client.GetBestForWorkAsync(
                "/authors/OL60038A"));

        Assert.Equal(
            0,
            httpHandler.CallCount);
    }

    private static HttpResponseMessage
        CreateJsonResponse(
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

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        protected override Task<HttpResponseMessage>
            SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            CallCount++;
            RequestUris.Add(request.RequestUri);
            CancellationToken = cancellationToken;

            return Task.FromResult(
                responseFactory(request));
        }
    }
}