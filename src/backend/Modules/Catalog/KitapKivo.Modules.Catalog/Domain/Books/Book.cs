using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Domain.Sources;
using KitapKivo.Modules.Catalog.Domain.Categories;
namespace KitapKivo.Modules.Catalog.Domain.Books;

internal sealed class Book
{
    private Book()
    {
    }

    public Book(
        string title,
        string originalLanguageCode,
        string? originalTitle = null,
        string? description = null,
        int? firstPublishedYear = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalLanguageCode);

        if (firstPublishedYear is < 0 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(firstPublishedYear));
        }

        Id = Guid.NewGuid();
        Title = title.Trim();
        OriginalTitle = NormalizeOptionalValue(originalTitle);
        Description = NormalizeOptionalValue(description);
        OriginalLanguageCode = originalLanguageCode
            .Trim()
            .ToLowerInvariant();
        FirstPublishedYear = firstPublishedYear;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? OriginalTitle { get; private set; }

    public string? Description { get; private set; }

    public string OriginalLanguageCode { get; private set; } = string.Empty;

    public int? FirstPublishedYear { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<BookAuthor> Authors { get; } = [];

    public ICollection<Edition> Editions { get; } = [];
    public ICollection<BookCategory> Categories { get; } = [];

    public ICollection<ExternalReference> ExternalReferences { get; } = [];

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}