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
        const string editionsJson = """
            {
              "size": 1,
              "entries": [
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
                  "covers": [
                    -1,
                    12345
                  ],
                  "publishers": [
                    " ",
                    "  Ace  "
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/works/OL45804W/editions.json" =>
                        CreateJsonResponse(editionsJson),

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
            1,
            httpHandler.CallCount);

        Assert.Single(
            httpHandler.RequestUris);

        Assert.Equal(
            "/works/OL45804W/editions.json",
            httpHandler.RequestUris[0]
                ?.AbsolutePath);

        Assert.Equal(
            "?limit=20",
            httpHandler.RequestUris[0]
                ?.Query);

        Assert.True(
            httpHandler.CancellationToken
                .CanBeCanceled);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldSelectHighestQualityEdition()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 3,
              "entries": [
                {
                  "key": "/books/OL1000001M",
                  "title": "Incomplete Edition",
                  "languages": [
                    {
                      "key": "/languages/eng"
                    }
                  ],
                  "publish_date": "2025"
                },
                {
                  "key": "/books/OL1000002M",
                  "title": "English Premium Edition",
                  "languages": [
                    {
                      "key": "/languages/eng"
                    }
                  ],
                  "physical_format": "Hardcover",
                  "isbn_10": [
                    "0441172717"
                  ],
                  "isbn_13": [
                    "9780441172719"
                  ],
                  "number_of_pages": 896,
                  "publish_date": "1999",
                  "covers": [
                    11111
                  ],
                  "publishers": [
                    "Ace"
                  ]
                },
                {
                  "key": "/books/OL1000003M",
                  "title": "Dune: Çöl Gezegeni",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "physical_format": "Paperback",
                  "isbn_13": [
                    "9789752112345"
                  ],
                  "number_of_pages": 712,
                  "publish_date": "2023",
                  "covers": [
                    22222
                  ],
                  "publishers": [
                    "İthaki Yayınları"
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/works/OL45804W/editions.json" =>
                        CreateJsonResponse(editionsJson),

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
        ExternalBookEditionDetails details =
            Assert.IsType<ExternalBookEditionDetails>(
                await client.GetBestForWorkAsync(
                    "/works/OL45804W"));

        // Assert
        Assert.Equal(
            "/books/OL1000003M",
            details.ExternalId);

        Assert.Equal(
            "Dune: Çöl Gezegeni",
            details.Title);

        Assert.Equal(
            "tur",
            details.LanguageCode);

        Assert.Equal(
            "9789752112345",
            details.Isbn13);

        Assert.Equal(
            712,
            details.PageCount);

        Assert.Equal(
            2023,
            details.PublicationYear);

        Assert.Equal(
            "https://covers.openlibrary.org/" +
            "b/id/22222-L.jpg",
            details.CoverUrl);

        Assert.Equal(
            "İthaki Yayınları",
            details.PublisherName);

        Assert.Equal(
            1,
            httpHandler.CallCount);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldReturnNull_WhenEditionIsMissing()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 0,
              "entries": []
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.AbsolutePath switch
                {
                    "/works/OL45804W/editions.json" =>
                        CreateJsonResponse(editionsJson),

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
            1,
            httpHandler.CallCount);

        Assert.Equal(
            "/works/OL45804W/editions.json",
            httpHandler.RequestUris[0]
                ?.AbsolutePath);

        Assert.Equal(
            "?limit=20",
            httpHandler.RequestUris[0]
                ?.Query);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldReturnNull_WhenWorkEditionsDoNotExist()
    {
        // Arrange
        var httpHandler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(
                HttpStatusCode.NotFound));

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
            "/works/OL45804W/editions.json",
            httpHandler.RequestUris[0]
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