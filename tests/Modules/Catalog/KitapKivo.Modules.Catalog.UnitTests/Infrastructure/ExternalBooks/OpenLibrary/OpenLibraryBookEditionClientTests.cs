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
        GetBestForWorkAsync_ShouldMapTurkishEdition()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 1,
              "entries": [
                {
                  "key": "/books/OL7353617M",
                  "title": "  Suç ve Ceza  ",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "physical_format": "  Paperback  ",
                  "isbn_10": [
                    "invalid",
                    " 6254052477 "
                  ],
                  "isbn_13": [
                    "invalid",
                    "9786254052476"
                  ],
                  "number_of_pages": 704,
                  "publish_date": "2021",
                  "covers": [
                    -1,
                    12345
                  ],
                  "publishers": [
                    " ",
                    "  Türkiye İş Bankası Kültür Yayınları  "
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(editionsJson));

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

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
            "Suç ve Ceza",
            details.Title);

        Assert.Equal(
            "tur",
            details.LanguageCode);

        Assert.Equal(
            "Paperback",
            details.Format);

        Assert.Equal(
            "6254052477",
            details.Isbn10);

        Assert.Equal(
            "9786254052476",
            details.Isbn13);

        Assert.Equal(
            704,
            details.PageCount);

        Assert.Equal(
            2021,
            details.PublicationYear);

        Assert.Equal(
            "https://covers.openlibrary.org/" +
            "b/id/12345-L.jpg",
            details.CoverUrl);

        Assert.Equal(
            "Türkiye İş Bankası Kültür Yayınları",
            details.PublisherName);

        Assert.Single(
            httpHandler.RequestUris);

        Assert.Equal(
            "?limit=100&offset=0",
            httpHandler.RequestUris[0]
                ?.Query);

        Assert.True(
            httpHandler.CancellationToken
                .CanBeCanceled);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldPreferConfiguredPublisherOrder()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 3,
              "entries": [
                {
                  "key": "/books/OL1000001M",
                  "title": "Suç ve Ceza - Yeni Baskı",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "isbn_13": [
                    "9789750719386"
                  ],
                  "publish_date": "2025",
                  "covers": [
                    11111
                  ],
                  "publishers": [
                    "Can Yayınları"
                  ]
                },
                {
                  "key": "/books/OL1000002M",
                  "title": "Suç ve Ceza",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "isbn_13": [
                    "9786053607010"
                  ],
                  "publish_date": "2018",
                  "covers": [
                    22222
                  ],
                  "publishers": [
                    "Türkiye İş Bankası Kültür Yayınları"
                  ]
                },
                {
                  "key": "/books/OL1000003M",
                  "title": "Suç ve Ceza",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "isbn_13": [
                    "9789753638028"
                  ],
                  "publish_date": "2024",
                  "covers": [
                    33333
                  ],
                  "publishers": [
                    "YKY"
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(editionsJson));

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

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
            "/books/OL1000002M",
            details.ExternalId);

        Assert.Equal(
            "Türkiye İş Bankası Kültür Yayınları",
            details.PublisherName);
    }

    [Fact]
    public async Task
        GetBestEditionsForWorkAsync_ShouldReturnOnlyTurkishEdition()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 2,
              "entries": [
                {
                  "key": "/books/OL1000011M",
                  "title": "Crime and Punishment",
                  "languages": [
                    {
                      "key": "/languages/eng"
                    }
                  ],
                  "covers": [
                    11111
                  ],
                  "publishers": [
                    "Penguin"
                  ]
                },
                {
                  "key": "/books/OL1000012M",
                  "title": "Suç ve Ceza",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "covers": [
                    22222
                  ],
                  "publishers": [
                    "İletişim Yayınları"
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(editionsJson));

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        // Act
        IReadOnlyList<ExternalBookEditionDetails>
            editions =
                await client
                    .GetBestEditionsForWorkAsync(
                        "/works/OL45804W");

        // Assert
        ExternalBookEditionDetails edition =
            Assert.Single(editions);

        Assert.Equal(
            "/books/OL1000012M",
            edition.ExternalId);

        Assert.Equal(
            "tur",
            edition.LanguageCode);

        Assert.Equal(
            "Suç ve Ceza",
            edition.Title);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldReturnNull_WhenOnlyForeignEditionExists()
    {
        // Arrange
        const string editionsJson = """
            {
              "size": 1,
              "entries": [
                {
                  "key": "/books/OL1000021M",
                  "title": "Crime and Punishment",
                  "languages": [
                    {
                      "key": "/languages/eng"
                    }
                  ],
                  "covers": [
                    11111
                  ],
                  "publishers": [
                    "Penguin"
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            _ => CreateJsonResponse(editionsJson));

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

        var client =
            new OpenLibraryBookEditionClient(
                httpClient);

        // Act
        ExternalBookEditionDetails? details =
            await client.GetBestForWorkAsync(
                "/works/OL45804W");

        // Assert
        Assert.Null(details);
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldContinueUntilTurkishEditionIsFound()
    {
        // Arrange
        const string firstPageJson = """
            {
              "size": 101,
              "entries": [
                {
                  "key": "/books/OL1000031M",
                  "title": "Foreign Edition",
                  "languages": [
                    {
                      "key": "/languages/eng"
                    }
                  ]
                }
              ]
            }
            """;

        const string secondPageJson = """
            {
              "size": 101,
              "entries": [
                {
                  "key": "/books/OL1000032M",
                  "title": "Türkçe Baskı",
                  "languages": [
                    {
                      "key": "/languages/tur"
                    }
                  ],
                  "publishers": [
                    "Metis Yayınları"
                  ]
                }
              ]
            }
            """;

        var httpHandler = new StubHttpMessageHandler(
            request =>
                request.RequestUri?.Query switch
                {
                    "?limit=100&offset=0" =>
                        CreateJsonResponse(
                            firstPageJson),

                    "?limit=100&offset=100" =>
                        CreateJsonResponse(
                            secondPageJson),

                    _ => new HttpResponseMessage(
                        HttpStatusCode.NotFound)
                });

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

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
            "/books/OL1000032M",
            details.ExternalId);

        Assert.Equal(
            2,
            httpHandler.CallCount);

        Assert.Equal(
            "?limit=100&offset=100",
            httpHandler.RequestUris[1]
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

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

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
    }

    [Fact]
    public async Task
        GetBestForWorkAsync_ShouldThrow_WhenWorkIdIsInvalid()
    {
        // Arrange
        var httpHandler = new StubHttpMessageHandler(
            _ => throw new InvalidOperationException(
                "HTTP request should not be sent."));

        using HttpClient httpClient =
            CreateHttpClient(httpHandler);

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

    private static HttpClient CreateHttpClient(
        HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://openlibrary.org/")
        };
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