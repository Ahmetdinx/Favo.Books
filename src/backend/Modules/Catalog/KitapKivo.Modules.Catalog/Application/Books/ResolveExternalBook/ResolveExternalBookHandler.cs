using System.Text.RegularExpressions;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Domain.Authors;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Sources;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application
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

    private readonly CatalogDbContext _dbContext;

    private readonly IExternalBookDetailsClient
        _detailsClient;

    public ResolveExternalBookHandler(
        CatalogDbContext dbContext,
        IExternalBookDetailsClient detailsClient)
    {
        _dbContext = dbContext;
        _detailsClient = detailsClient;
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
            return ResolveExternalBookResult.Existing(
                existingBookId.Value);
        }

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
                return ResolveExternalBookResult.Existing(
                    existingBookId.Value);
            }
        }

        string originalLanguageCode =
            ResolveOriginalLanguageCode(details);

        Book book = new(
            details.Title,
            originalLanguageCode,
            null,
            details.Description,
            details.FirstPublishedYear);

        Dictionary<string, Guid> resolvedAuthorIds =
            new(StringComparer.OrdinalIgnoreCase);

        int displayOrder = 0;

        foreach (ExternalBookAuthor externalAuthor
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

        ExternalReference bookReference =
            ExternalReference.CreateForBook(
                book.Id,
                source,
                resolvedExternalId);

        book.ExternalReferences.Add(
            bookReference);

        _dbContext.Books.Add(book);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return ResolveExternalBookResult.Created(
            book.Id);
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
            .Select(reference => reference.BookId)
            .SingleOrDefaultAsync(cancellationToken);
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
                "Only the OpenLibrary provider is supported.",
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
                "Open Library work identifier is invalid.",
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
        if (string.IsNullOrWhiteSpace(externalId))
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
}