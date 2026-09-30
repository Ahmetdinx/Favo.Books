using Favo.Books.Modules.Catalog.Domain.Books;

namespace Favo.Books.Modules.Catalog.Domain.Categories;

internal sealed class BookCategory
{
    private BookCategory()
    {
    }

    public BookCategory(
        Guid bookId,
        Guid categoryId)
    {
        if (bookId == Guid.Empty)
        {
            throw new ArgumentException(
                "Book identifier cannot be empty.",
                nameof(bookId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category identifier cannot be empty.",
                nameof(categoryId));
        }

        BookId = bookId;
        CategoryId = categoryId;
    }

    public Guid BookId { get; private set; }

    public Book Book { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;
}