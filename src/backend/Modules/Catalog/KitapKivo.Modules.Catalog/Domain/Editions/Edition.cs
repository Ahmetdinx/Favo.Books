using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Publishers;
using KitapKivo.Modules.Catalog.Domain.Sources;

namespace KitapKivo.Modules.Catalog.Domain.Editions;

internal sealed class Edition
{
    private Edition()
    {
    }

    public Edition(
        Guid bookId,
        string title,
        string languageCode,
        EditionFormat format,
        Guid? publisherId = null,
        string? isbn10 = null,
        string? isbn13 = null,
        int? pageCount = null,
        int? publicationYear = null,
        string? coverImageUrl = null)
    {
        if (bookId == Guid.Empty)
        {
            throw new ArgumentException(
                "Book identifier cannot be empty.",
                nameof(bookId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        if (publisherId == Guid.Empty)
        {
            throw new ArgumentException(
                "Publisher identifier cannot be empty.",
                nameof(publisherId));
        }

        if (pageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        }

        if (publicationYear is < 0 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(publicationYear));
        }

        Id = Guid.NewGuid();
        BookId = bookId;
        PublisherId = publisherId;
        Title = title.Trim();
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        Format = format;
        Isbn10 = NormalizeIsbn(isbn10);
        Isbn13 = NormalizeIsbn(isbn13);
        PageCount = pageCount;
        PublicationYear = publicationYear;
        CoverImageUrl = NormalizeOptionalValue(coverImageUrl);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public Book Book { get; private set; } = null!;

    public Guid? PublisherId { get; private set; }

    public Publisher? Publisher { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string LanguageCode { get; private set; } = string.Empty;

    public EditionFormat Format { get; private set; }

    public string? Isbn10 { get; private set; }

    public string? Isbn13 { get; private set; }

    public int? PageCount { get; private set; }

    public int? PublicationYear { get; private set; }

    public string? CoverImageUrl { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<ExternalReference> ExternalReferences { get; } = [];

    private static string? NormalizeIsbn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}