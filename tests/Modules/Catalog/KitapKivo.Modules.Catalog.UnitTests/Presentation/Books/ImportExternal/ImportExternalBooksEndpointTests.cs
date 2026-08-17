using KitapKivo.Modules.Catalog.Presentation
    .Books.ImportExternal;

namespace KitapKivo.Modules.Catalog.UnitTests
    .Presentation.Books.ImportExternal;

public sealed class ImportExternalBooksEndpointTests
{
    [Fact]
    public void Validate_WhenRequestIsValid_ShouldReturnNoErrors()
    {
        // Act
        Dictionary<string, string[]> errors =
            ImportExternalBooksEndpoint.Validate(
                " OpenLibrary ",
                " fantasy ",
                1,
                10);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenRequiredValuesAreMissing_ShouldReturnErrors()
    {
        // Act
        Dictionary<string, string[]> errors =
            ImportExternalBooksEndpoint.Validate(
                null,
                null,
                1,
                10);

        // Assert
        Assert.True(
            errors.ContainsKey("provider"));

        Assert.True(
            errors.ContainsKey("query"));
    }

    [Fact]
    public void Validate_WhenProviderAndQueryAreInvalid_ShouldReturnErrors()
    {
        // Arrange
        string longQuery = new(
            'a',
            201);

        // Act
        Dictionary<string, string[]> errors =
            ImportExternalBooksEndpoint.Validate(
                "UnsupportedProvider",
                longQuery,
                1,
                10);

        // Assert
        Assert.True(
            errors.ContainsKey("provider"));

        Assert.True(
            errors.ContainsKey("query"));
    }

    [Fact]
    public void Validate_WhenPagingIsInvalid_ShouldReturnErrors()
    {
        // Act
        Dictionary<string, string[]> errors =
            ImportExternalBooksEndpoint.Validate(
                "OpenLibrary",
                "fantasy",
                0,
                21);

        // Assert
        Assert.True(
            errors.ContainsKey("page"));

        Assert.True(
            errors.ContainsKey("pageSize"));
    }
}