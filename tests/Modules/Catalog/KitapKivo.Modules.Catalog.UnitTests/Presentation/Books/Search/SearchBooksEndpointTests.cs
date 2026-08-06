using KitapKivo.Modules.Catalog.Presentation.Books.Search;

namespace KitapKivo.Modules.Catalog.UnitTests.Presentation.Books.Search;

public sealed class SearchBooksEndpointTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenInputIsValid()
    {
        // Act
        Dictionary<string, string[]> errors =
            SearchBooksEndpoint.Validate(
                query: " dune ",
                page: 1,
                pageSize: 20);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnErrors_WhenInputIsInvalid()
    {
        // Act
        Dictionary<string, string[]> errors =
            SearchBooksEndpoint.Validate(
                query: " ",
                page: 0,
                pageSize: 51);

        // Assert
        Assert.Equal(3, errors.Count);

        Assert.Equal(
            "Arama metni zorunludur.",
            Assert.Single(errors["query"]));

        Assert.Equal(
            "Sayfa numarası en az 1 olmalıdır.",
            Assert.Single(errors["page"]));

        Assert.Equal(
            "Sayfa boyutu 1 ile 50 arasında olmalıdır.",
            Assert.Single(errors["pageSize"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenQueryIsTooLong()
    {
        // Arrange
        string query = new('a', 201);

        // Act
        Dictionary<string, string[]> errors =
            SearchBooksEndpoint.Validate(
                query,
                page: 1,
                pageSize: 20);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Arama metni en fazla 200 karakter olabilir.",
            Assert.Single(errors["query"]));
    }
}