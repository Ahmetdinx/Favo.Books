using KitapKivo.Modules.Catalog.Domain.Categories;

namespace KitapKivo.Modules.Catalog.UnitTests.Domain.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void Constructor_ShouldCreateCategory_WhenValuesAreValid()
    {
        // Arrange
        DateTimeOffset beforeCreation =
            DateTimeOffset.UtcNow;

        // Act
        var category = new Category(
            "  Bilim Kurgu  ",
            "  BILIM-KURGU  ");

        DateTimeOffset afterCreation =
            DateTimeOffset.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Bilim Kurgu", category.Name);
        Assert.Equal("bilim-kurgu", category.Slug);

        Assert.InRange(
            category.CreatedAtUtc,
            beforeCreation,
            afterCreation);

        Assert.Empty(category.Books);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsEmpty(
        string name)
    {
        // Act and assert
        Assert.Throws<ArgumentException>(() =>
            new Category(name, "roman"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenSlugIsEmpty(
        string slug)
    {
        // Act and assert
        Assert.Throws<ArgumentException>(() =>
            new Category("Roman", slug));
    }

    [Theory]
    [InlineData("bilim kurgu")]
    [InlineData("-roman")]
    [InlineData("roman-")]
    [InlineData("roman--klasik")]
    [InlineData("roman_klasik")]
    public void Constructor_ShouldThrow_WhenSlugIsInvalid(
        string slug)
    {
        // Act and assert
        Assert.Throws<ArgumentException>(() =>
            new Category("Kategori", slug));
    }
}