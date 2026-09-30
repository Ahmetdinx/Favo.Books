using Favo.Books.Modules.Catalog.Domain.Authors;

namespace Favo.Books.Modules.Catalog.Domain.Books;

internal sealed class BookAuthor
{
    private BookAuthor()
    {
    }

    public BookAuthor(
        Guid bookId,
        Guid authorId,
        int displayOrder)
    {
        if (bookId == Guid.Empty)
        {
            throw new ArgumentException(
                "Book identifier cannot be empty.",
                nameof(bookId));
        }

        if (authorId == Guid.Empty)
        {
            throw new ArgumentException(
                "Author identifier cannot be empty.",
                nameof(authorId));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder));
        }

        BookId = bookId;
        AuthorId = authorId;
        DisplayOrder = displayOrder;
    }

    public Guid BookId { get; private set; }

    public Book Book { get; private set; } = null!;

    public Guid AuthorId { get; private set; }

    public Author Author { get; private set; } = null!;

    public int DisplayOrder { get; private set; }
}