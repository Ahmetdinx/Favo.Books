using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application
    .Books.GetExternalDetails;

namespace Favo.Books.Modules.Catalog.UnitTests
    .Application.Books.GetExternalDetails;

public sealed class GetExternalBookDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnDetails()
    {
        // Arrange
        var expectedDetails = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL45804W",
            "Dune",
            "Epic science fiction novel.",
            [
                new ExternalBookAuthor(
                    "/authors/OL60038A",
                    "Frank Herbert")
            ],
            1965,
            "https://example.com/dune.jpg",
            ["9780441013593"],
            ["eng"],
            ["Science Fiction"]);

        var detailsClient =
            new StubExternalBookDetailsClient(
                expectedDetails);

        var handler =
            new GetExternalBookDetailsHandler(
                detailsClient);

        var query =
            new GetExternalBookDetailsQuery(
                "/works/OL45804W");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act
        ExternalBookDetails? result =
            await handler.HandleAsync(
                query,
                cancellationTokenSource.Token);

        // Assert
        Assert.Same(expectedDetails, result);

        Assert.Equal(
            "/works/OL45804W",
            detailsClient.ExternalId);

        Assert.Equal(
            cancellationTokenSource.Token,
            detailsClient.CancellationToken);

        Assert.Equal(1, detailsClient.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenQueryIsNull()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        var handler =
            new GetExternalBookDetailsHandler(
                detailsClient);

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!));

        Assert.Equal(0, detailsClient.CallCount);
    }

    private sealed class StubExternalBookDetailsClient(
        ExternalBookDetails? result)
        : IExternalBookDetailsClient
    {
        public int CallCount { get; private set; }

        public string? ExternalId { get; private set; }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<ExternalBookDetails?> GetByIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalId = externalId;
            CancellationToken = cancellationToken;

            return Task.FromResult(result);
        }
    }
}