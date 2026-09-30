using Favo.Books.Modules.Catalog.Domain.Categories;

namespace Favo.Books.Modules.Catalog.UnitTests.Domain.Categories;

public sealed class BookCategoryTests
{
    [Fact]
    public void Constructor_ShouldCreateRelation_WhenIdsAreValid()
    {
        // Arrange
        Guid bookId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        // Act
        var bookCategory = new BookCategory(
            bookId,
            categoryId);

        // Assert
        Assert.Equal(bookId, bookCategory.BookId);
        Assert.Equal(categoryId, bookCategory.CategoryId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenBookIdIsEmpty()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();

        // Act and assert
        Assert.Throws<ArgumentException>(() =>
            new BookCategory(
                Guid.Empty,
                categoryId));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCategoryIdIsEmpty()
    {
        // Arrange
        Guid bookId = Guid.NewGuid();

        // Act and assert
        Assert.Throws<ArgumentException>(() =>
            new BookCategory(
                bookId,
                Guid.Empty));
    }
}