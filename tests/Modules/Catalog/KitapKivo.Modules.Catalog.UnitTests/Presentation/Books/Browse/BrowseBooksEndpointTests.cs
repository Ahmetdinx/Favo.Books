using KitapKivo.Modules.Catalog.Application.Books.Browse;
using KitapKivo.Modules.Catalog.Presentation.Books.Browse;

namespace KitapKivo.Modules.Catalog.UnitTests.Presentation.Books.Browse;

public sealed class BrowseBooksEndpointTests
{
    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenInputIsValid()
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: " suç ve ceza ",
                searchField: "title",
                category: "klasikler",
                page: 1,
                pageSize: 24,
                sort: "title",
                parsedSearchField:
                    CatalogBookSearchField.Title,
                parsedSort:
                    CatalogBookSort.Title);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldAllowEmptyQuery()
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: null,
                searchField: null,
                category: null,
                page: 1,
                pageSize: 24,
                sort: null,
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenQueryIsTooLong()
    {
        // Arrange
        string query = new('a', 201);

        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query,
                searchField: null,
                category: null,
                page: 1,
                pageSize: 24,
                sort: null,
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Arama metni en fazla 200 karakter olabilir.",
            Assert.Single(errors["query"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenSearchFieldIsInvalid()
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: "dune",
                searchField: "publisher",
                category: null,
                page: 1,
                pageSize: 24,
                sort: null,
                parsedSearchField: null,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Arama alanı all, title veya author olmalıdır.",
            Assert.Single(errors["searchField"]));
    }

    [Theory]
    [InlineData("bilim kurgu")]
    [InlineData("-klasikler")]
    [InlineData("klasikler-")]
    [InlineData("bilim--kurgu")]
    [InlineData("bilim_kurgu")]
    public void Validate_ShouldReturnError_WhenCategoryIsInvalid(
        string category)
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: null,
                searchField: null,
                category,
                page: 1,
                pageSize: 24,
                sort: null,
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Kategori geçerli bir kategori slug değeri olmalıdır.",
            Assert.Single(errors["category"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenPageIsInvalid()
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: null,
                searchField: null,
                category: null,
                page: 0,
                pageSize: 24,
                sort: null,
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Sayfa numarası en az 1 olmalıdır.",
            Assert.Single(errors["page"]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ShouldReturnError_WhenPageSizeIsInvalid(
        int pageSize)
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: null,
                searchField: null,
                category: null,
                page: 1,
                pageSize,
                sort: null,
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort:
                    CatalogBookSort.RecentlyAdded);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Sayfa boyutu 1 ile 50 arasında olmalıdır.",
            Assert.Single(errors["pageSize"]));
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenSortIsInvalid()
    {
        // Act
        Dictionary<string, string[]> errors =
            BrowseBooksEndpoint.Validate(
                query: null,
                searchField: null,
                category: null,
                page: 1,
                pageSize: 24,
                sort: "rating",
                parsedSearchField:
                    CatalogBookSearchField.All,
                parsedSort: null);

        // Assert
        Assert.Single(errors);

        Assert.Equal(
            "Sıralama recently-added, title veya first-published-newest olmalıdır.",
            Assert.Single(errors["sort"]));
    }
}