using Favo.Books.Modules.Catalog.Presentation
    .Books.GetExternalDetails;

namespace Favo.Books.Modules.Catalog.UnitTests
    .Presentation.Books.GetExternalDetails;

public sealed class GetExternalBookDetailsEndpointTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenRequestIsValid()
    {
        // Act
        Dictionary<string, string[]> errors =
            GetExternalBookDetailsEndpoint.Validate(
                " openlibrary ",
                " works/ol45804w ");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenProviderIsMissing()
    {
        // Act
        Dictionary<string, string[]> errors =
            GetExternalBookDetailsEndpoint.Validate(
                null,
                "/works/OL45804W");

        // Assert
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("provider"));

        Assert.Equal(
            "Kitap sağlayıcısı zorunludur.",
            Assert.Single(errors["provider"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenProviderIsUnsupported()
    {
        // Act
        Dictionary<string, string[]> errors =
            GetExternalBookDetailsEndpoint.Validate(
                "GoogleBooks",
                "/works/OL45804W");

        // Assert
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("provider"));

        Assert.Equal(
            "Yalnızca OpenLibrary sağlayıcısı desteklenmektedir.",
            Assert.Single(errors["provider"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenExternalIdIsMissing()
    {
        // Act
        Dictionary<string, string[]> errors =
            GetExternalBookDetailsEndpoint.Validate(
                "OpenLibrary",
                " ");

        // Assert
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("externalId"));

        Assert.Equal(
            "Dış kitap kimliği zorunludur.",
            Assert.Single(errors["externalId"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenExternalIdIsInvalid()
    {
        // Act
        Dictionary<string, string[]> errors =
            GetExternalBookDetailsEndpoint.Validate(
                "OpenLibrary",
                "/authors/OL60038A");

        // Assert
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("externalId"));

        Assert.Equal(
            "Open Library kitap kimliği " +
            "'/works/OL...W' biçiminde olmalıdır.",
            Assert.Single(errors["externalId"]));
    }
}