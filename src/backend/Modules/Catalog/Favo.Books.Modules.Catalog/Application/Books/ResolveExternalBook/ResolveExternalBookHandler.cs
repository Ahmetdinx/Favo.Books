using System.Text.RegularExpressions;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application
    .Categories.Mapping;
using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Categories;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Publishers;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application
    .Books.ResolveExternal;

public sealed class ResolveExternalBookHandler
{
    private const string SupportedProvider =
        "OpenLibrary";

    private static readonly Regex WorkIdPattern = new(
        @"^/?works/(OL\d+W)$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex AuthorIdPattern = new(
        @"^/?authors/(OL\d+A)$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex EditionIdPattern = new(
        @"^/?books/(OL\d+M)$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private readonly CatalogDbContext _dbContext;

    private readonly IExternalBookDetailsClient
        _detailsClient;

    private readonly IExternalBookEditionClient
        _editionClient;
    private readonly IApprovedNovelCatalog _approvedNovels;

    public ResolveExternalBookHandler(
        CatalogDbContext dbContext,
        IExternalBookDetailsClient detailsClient,
        IExternalBookEditionClient editionClient,
        IApprovedNovelCatalog? approvedNovels = null)
    {
        _dbContext = dbContext;
        _detailsClient = detailsClient;
        _editionClient = editionClient;
        _approvedNovels = approvedNovels ?? ApprovedNovelCatalog.Default;
    }

    public async Task<ResolveExternalBookResult>
        HandleAsync(
            ResolveExternalBookCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        CatalogSource source =
            ResolveSource(command.Provider);

        string requestedExternalId =
            NormalizeWorkId(command.ExternalId);

        Guid? existingBookId =
            await FindBookIdAsync(
                source,
                requestedExternalId,
                cancellationToken);

        if (existingBookId.HasValue)
        {
            return await ExistingIfEligibleAsync(existingBookId.Value, cancellationToken);
        }

        if (_approvedNovels.Find(requestedExternalId) is null)
            return ResolveExternalBookResult.Rejected("novel_review_required");

        ExternalBookDetails? details =
            await _detailsClient.GetByIdAsync(
                requestedExternalId,
                cancellationToken);

        if (details is null)
        {
            return ResolveExternalBookResult.NotFound();
        }

        string resolvedExternalId =
            NormalizeWorkId(details.ExternalId);

        if (!string.Equals(
                requestedExternalId,
                resolvedExternalId,
                StringComparison.Ordinal))
        {
            existingBookId =
                await FindBookIdAsync(
                    source,
                    resolvedExternalId,
                    cancellationToken);

            if (existingBookId.HasValue)
            {
                return await ExistingIfEligibleAsync(existingBookId.Value, cancellationToken);
            }
        }

        var approval = _approvedNovels.Find(resolvedExternalId);
        if (approval is null) return ResolveExternalBookResult.Rejected("novel_review_required");
        if (details.Provider != SupportedProvider || details.Title.Trim() != approval.WorkTitle
            || details.Authors.Count == 0 || details.Authors.Any(a => !approval.AuthorNames.Contains(a.Name.Trim(), StringComparer.Ordinal)))
            return ResolveExternalBookResult.Rejected("reviewed_identity_mismatch");

        IReadOnlyList<ExternalBookEditionDetails>
          editionDetails =
              await _editionClient
                  .GetBestEditionsForWorkAsync(
                      resolvedExternalId,
                      cancellationToken);

        ExternalBookEditionDetails?
            selectedTurkishEdition =
                editionDetails.FirstOrDefault(
                    edition =>
                        IsTurkishLanguageCode(edition.LanguageCode)
                        && !string.IsNullOrWhiteSpace(edition.CoverUrl)
                        && edition.Provider == SupportedProvider
                        && edition.Title?.Trim() == approval.TurkishTitle);

        string? turkishTitle =
            NormalizeOptionalValue(
                selectedTurkishEdition?.Title);

        if (selectedTurkishEdition is null ||
            turkishTitle is null)
        {
            return ResolveExternalBookResult.Rejected("covered_turkish_edition_required");
        }

        editionDetails = [selectedTurkishEdition];

        string originalLanguageCode =
            ResolveOriginalLanguageCode(details);

        string? originalTitle =
            string.Equals(
                turkishTitle,
                details.Title,
                StringComparison.OrdinalIgnoreCase)
                ? null
                : details.Title;

        Book book = new(
            turkishTitle,
            originalLanguageCode,
            originalTitle,
            details.Description,
            details.FirstPublishedYear);

        Dictionary<string, Guid> resolvedAuthorIds =
            new(StringComparer.OrdinalIgnoreCase);

        int displayOrder = 0;

        foreach (
            ExternalBookAuthor externalAuthor
            in details.Authors)
        {
            if (string.IsNullOrWhiteSpace(
                    externalAuthor.Name))
            {
                continue;
            }

            string normalizedAuthorName =
                externalAuthor.Name.Trim();

            string? authorExternalId =
                NormalizeAuthorId(
                    externalAuthor.ExternalId);

            string authorKey =
                authorExternalId ??
                $"name:{normalizedAuthorName}";

            if (resolvedAuthorIds.ContainsKey(
                    authorKey))
            {
                continue;
            }

            Guid authorId =
                await ResolveAuthorIdAsync(
                    source,
                    authorExternalId,
                    normalizedAuthorName,
                    cancellationToken);

            resolvedAuthorIds.Add(
                authorKey,
                authorId);

            BookAuthor bookAuthor = new(
                book.Id,
                authorId,
                displayOrder);

            book.Authors.Add(bookAuthor);

            displayOrder++;
        }

        string[] mappedCategorySlugs =
            SubjectCategoryMapper
                .Map(details.Subjects)
                .ToArray();

        if (mappedCategorySlugs.Length > 0)
        {
            Guid[] categoryIds =
                await _dbContext.Categories
                    .AsNoTracking()
                    .Where(category =>
                        mappedCategorySlugs.Contains(
                            category.Slug))
                    .OrderBy(category =>
                        category.Slug)
                    .Select(category =>
                        category.Id)
                    .ToArrayAsync(
                        cancellationToken);

            foreach (Guid categoryId in categoryIds)
            {
                BookCategory bookCategory = new(
                    book.Id,
                    categoryId);

                book.Categories.Add(bookCategory);
            }
        }
        HashSet<string> addedEditionExternalIds =
            new(StringComparer.OrdinalIgnoreCase);

        HashSet<string> addedEditionIsbns =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (
            ExternalBookEditionDetails editionDetail
            in editionDetails)
        {
            string normalizedExternalId =
                NormalizeEditionId(
                    editionDetail.ExternalId);

            string? normalizedIsbn10 =
                NormalizeIsbn(
                    editionDetail.Isbn10);

            string? normalizedIsbn13 =
                NormalizeIsbn(
                    editionDetail.Isbn13);

            bool externalIdAlreadyAdded =
                addedEditionExternalIds.Contains(
                    normalizedExternalId);

            bool isbnAlreadyAdded =
                (
                    normalizedIsbn10 is not null &&
                    addedEditionIsbns.Contains(
                        normalizedIsbn10)
                ) ||
                (
                    normalizedIsbn13 is not null &&
                    addedEditionIsbns.Contains(
                        normalizedIsbn13)
                );

            if (externalIdAlreadyAdded ||
                isbnAlreadyAdded)
            {
                continue;
            }

            addedEditionExternalIds.Add(
                normalizedExternalId);

            if (normalizedIsbn10 is not null)
            {
                addedEditionIsbns.Add(
                    normalizedIsbn10);
            }

            if (normalizedIsbn13 is not null)
            {
                addedEditionIsbns.Add(
                    normalizedIsbn13);
            }

            await AddEditionAsync(
                book,
                editionDetail,
                source,
                originalLanguageCode,
                cancellationToken);
        }

        ExternalReference bookReference =
            ExternalReference.CreateForBook(
                book.Id,
                source,
                resolvedExternalId);

        book.ExternalReferences.Add(
            bookReference);

        _dbContext.Books.Add(book);
        book.NovelReviews.Add(new NovelReview(book.Id, NovelDecision.Novel,
            "reviewed-import-v1", approval.SourceUrl, approval.EvidenceNote,
            "curated-import", "editorial-novel-v1"));

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return ResolveExternalBookResult.Created(
            book.Id);
    }

    private async Task<ResolveExternalBookResult> ExistingIfEligibleAsync(Guid id, CancellationToken ct)
        => await _dbContext.Books.EligibleNovels().AnyAsync(b => b.Id == id, ct)
            ? ResolveExternalBookResult.Existing(id)
            : ResolveExternalBookResult.Rejected("existing_book_not_eligible");

    private async Task AddEditionAsync(
        Book book,
        ExternalBookEditionDetails details,
        CatalogSource source,
        string fallbackLanguageCode,
        CancellationToken cancellationToken)
    {
        string editionExternalId =
            NormalizeEditionId(
                details.ExternalId);

        string editionTitle =
            NormalizeOptionalValue(details.Title)
            ?? book.Title;

        string languageCode =
            NormalizeOptionalValue(
                details.LanguageCode)
            ?.ToLowerInvariant()
            ?? fallbackLanguageCode;

        Guid? publisherId =
            await ResolvePublisherIdAsync(
                details.PublisherName,
                cancellationToken);

        int? pageCount =
            details.PageCount is > 0
                ? details.PageCount
                : null;

        int? publicationYear =
            details.PublicationYear is
                >= 0 and <= 9999
                    ? details.PublicationYear
                    : null;

        Edition edition = new(
            book.Id,
            editionTitle,
            languageCode,
            ResolveEditionFormat(
                details.Format),
            publisherId,
            details.Isbn10,
            details.Isbn13,
            pageCount,
            publicationYear,
            details.CoverUrl);

        ExternalReference editionReference =
            ExternalReference.CreateForEdition(
                edition.Id,
                source,
                editionExternalId);

        edition.ExternalReferences.Add(
            editionReference);

        book.Editions.Add(edition);
    }

    private async Task<Guid?> ResolvePublisherIdAsync(
        string? publisherName,
        CancellationToken cancellationToken)
    {
        string? normalizedPublisherName =
            NormalizeOptionalValue(
                publisherName);

        if (normalizedPublisherName is null)
        {
            return null;
        }

        string normalizedPublisherKey =
            normalizedPublisherName
                .ToUpperInvariant();

        Guid? trackedPublisherId =
    _dbContext.Publishers.Local
        .Where(publisher =>
            string.Equals(
                publisher.Name,
                normalizedPublisherName,
                StringComparison.OrdinalIgnoreCase))
        .Select(publisher =>
            (Guid?)publisher.Id)
        .FirstOrDefault();

        if (trackedPublisherId.HasValue)
        {
            return trackedPublisherId.Value;
        }

        Guid? existingPublisherId =
            await _dbContext.Publishers
                .AsNoTracking()
                .Where(publisher =>
                    publisher.Name.ToUpper() ==
                    normalizedPublisherKey)
                .OrderBy(publisher =>
                    publisher.CreatedAtUtc)
                .Select(publisher =>
                    (Guid?)publisher.Id)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (existingPublisherId.HasValue)
        {
            return existingPublisherId.Value;
        }

        Publisher publisher = new(
            normalizedPublisherName);

        _dbContext.Publishers.Add(publisher);

        return publisher.Id;
    }

    private async Task<Guid?> FindBookIdAsync(
        CatalogSource source,
        string externalId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ExternalReferences
            .AsNoTracking()
            .Where(reference =>
                reference.Source == source &&
                reference.ExternalId == externalId &&
                reference.BookId.HasValue)
            .Select(reference =>
                reference.BookId)
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private async Task<Guid> ResolveAuthorIdAsync(
        CatalogSource source,
        string? externalId,
        string authorName,
        CancellationToken cancellationToken)
    {
        if (externalId is not null)
        {
            Guid? existingAuthorId =
                await _dbContext.ExternalReferences
                    .AsNoTracking()
                    .Where(reference =>
                        reference.Source == source &&
                        reference.ExternalId ==
                            externalId &&
                        reference.AuthorId.HasValue)
                    .Select(reference =>
                        reference.AuthorId)
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (existingAuthorId.HasValue)
            {
                return existingAuthorId.Value;
            }
        }

        Author author = new(authorName);

        if (externalId is not null)
        {
            ExternalReference authorReference =
                ExternalReference.CreateForAuthor(
                    author.Id,
                    source,
                    externalId);

            author.ExternalReferences.Add(
                authorReference);
        }

        _dbContext.Authors.Add(author);

        return author.Id;
    }

    private static CatalogSource ResolveSource(
        string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            provider);

        if (!string.Equals(
                provider.Trim(),
                SupportedProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only the OpenLibrary provider " +
                "is supported.",
                nameof(provider));
        }

        return CatalogSource.OpenLibrary;
    }

    private static string NormalizeWorkId(
        string externalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalId);

        Match match = WorkIdPattern.Match(
            externalId.Trim());

        if (!match.Success)
        {
            throw new ArgumentException(
                "Open Library work identifier " +
                "is invalid.",
                nameof(externalId));
        }

        string workId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/works/{workId}";
    }

    private static string? NormalizeAuthorId(
        string? externalId)
    {
        if (string.IsNullOrWhiteSpace(
                externalId))
        {
            return null;
        }

        Match match = AuthorIdPattern.Match(
            externalId.Trim());

        if (!match.Success)
        {
            return null;
        }

        string authorId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/authors/{authorId}";
    }

    private static string NormalizeEditionId(
        string externalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalId);

        Match match = EditionIdPattern.Match(
            externalId.Trim());

        if (!match.Success)
        {
            throw new ArgumentException(
                "Open Library edition identifier " +
                "is invalid.",
                nameof(externalId));
        }

        string editionId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/books/{editionId}";
    }

    private static EditionFormat ResolveEditionFormat(
        string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return EditionFormat.Unknown;
        }

        string normalizedFormat =
            format.Trim().ToLowerInvariant();

        if (normalizedFormat.Contains(
                "hardcover",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "hardback",
                StringComparison.Ordinal))
        {
            return EditionFormat.Hardcover;
        }

        if (normalizedFormat.Contains(
                "paperback",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "softcover",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "soft cover",
                StringComparison.Ordinal))
        {
            return EditionFormat.Paperback;
        }

        if (normalizedFormat.Contains(
                "ebook",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "e-book",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "electronic",
                StringComparison.Ordinal))
        {
            return EditionFormat.Ebook;
        }

        if (normalizedFormat.Contains(
                "audiobook",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "audio book",
                StringComparison.Ordinal) ||
            normalizedFormat.Contains(
                "audio cd",
                StringComparison.Ordinal))
        {
            return EditionFormat.Audiobook;
        }

        return EditionFormat.Other;
    }
    private static bool IsTurkishLanguageCode(
    string? languageCode)
    {
        return string.Equals(
                   languageCode,
                   "tr",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   languageCode,
                   "tur",
                   StringComparison.OrdinalIgnoreCase);
    }
    private static string ResolveOriginalLanguageCode(
        ExternalBookDetails details)
    {
        string? languageCode =
            details.LanguageCodes
                .FirstOrDefault(code =>
                    !string.IsNullOrWhiteSpace(code))
                ?.Trim();

        return languageCode ?? "und";
    }
    private static string? NormalizeIsbn(
    string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Replace(
                "-",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                " ",
                string.Empty,
                StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
