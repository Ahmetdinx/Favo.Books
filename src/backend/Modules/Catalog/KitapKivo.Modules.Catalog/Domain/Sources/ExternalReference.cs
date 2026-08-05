using KitapKivo.Modules.Catalog.Domain.Authors;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Domain.Publishers;

namespace KitapKivo.Modules.Catalog.Domain.Sources;

internal sealed class ExternalReference
{
    private ExternalReference()
    {
    }

    private ExternalReference(
        CatalogSource source,
        string externalId,
        string? sourceUrl)
    {
        if (source == CatalogSource.Unknown)
        {
            throw new ArgumentException(
                "Catalog source must be specified.",
                nameof(source));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        Id = Guid.NewGuid();
        Source = source;
        ExternalId = externalId.Trim();
        SourceUrl = NormalizeOptionalValue(sourceUrl);
        LastSynchronizedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public CatalogSource Source { get; private set; }

    public string ExternalId { get; private set; } = string.Empty;

    public string? SourceUrl { get; private set; }

    public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

    public Guid? BookId { get; private set; }

    public Book? Book { get; private set; }

    public Guid? AuthorId { get; private set; }

    public Author? Author { get; private set; }

    public Guid? EditionId { get; private set; }

    public Edition? Edition { get; private set; }

    public Guid? PublisherId { get; private set; }

    public Publisher? Publisher { get; private set; }

    public static ExternalReference CreateForBook(
        Guid bookId,
        CatalogSource source,
        string externalId,
        string? sourceUrl = null)
    {
        EnsureIdentifierIsNotEmpty(bookId, nameof(bookId));

        return new ExternalReference(source, externalId, sourceUrl)
        {
            BookId = bookId
        };
    }

    public static ExternalReference CreateForAuthor(
        Guid authorId,
        CatalogSource source,
        string externalId,
        string? sourceUrl = null)
    {
        EnsureIdentifierIsNotEmpty(authorId, nameof(authorId));

        return new ExternalReference(source, externalId, sourceUrl)
        {
            AuthorId = authorId
        };
    }

    public static ExternalReference CreateForEdition(
        Guid editionId,
        CatalogSource source,
        string externalId,
        string? sourceUrl = null)
    {
        EnsureIdentifierIsNotEmpty(editionId, nameof(editionId));

        return new ExternalReference(source, externalId, sourceUrl)
        {
            EditionId = editionId
        };
    }

    public static ExternalReference CreateForPublisher(
        Guid publisherId,
        CatalogSource source,
        string externalId,
        string? sourceUrl = null)
    {
        EnsureIdentifierIsNotEmpty(publisherId, nameof(publisherId));

        return new ExternalReference(source, externalId, sourceUrl)
        {
            PublisherId = publisherId
        };
    }

    private static void EnsureIdentifierIsNotEmpty(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Entity identifier cannot be empty.",
                parameterName);
        }
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}