using KitapKivo.Modules.Catalog.Application.Books.Browse;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation.Books.Browse;

internal static class BrowseBooksEndpoint
{
    public static RouteGroupBuilder MapBrowseBooksEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapGet("/books", HandleAsync)
            .WithName("BrowseBooks")
            .Produces<BrowseBooksResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string? query,
        string? searchField,
        string? category,
        int? page,
        int? pageSize,
        string? sort,
        BrowseBooksHandler handler,
        CancellationToken cancellationToken)
    {
        int requestedPage = page ?? 1;
        int requestedPageSize = pageSize ?? 24;

        CatalogBookSearchField? parsedSearchField =
            ParseSearchField(searchField);

        CatalogBookSort? parsedSort =
            ParseSort(sort);

        Dictionary<string, string[]> errors = Validate(
            query,
            searchField,
            category,
            requestedPage,
            requestedPageSize,
            sort,
            parsedSearchField,
            parsedSort);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        BrowseBooksQuery browseQuery = new(
            query?.Trim(),
            parsedSearchField!.Value,
            category?.Trim(),
            requestedPage,
            requestedPageSize,
            parsedSort!.Value);

        BrowseBooksResult result =
            await handler.HandleAsync(
                browseQuery,
                cancellationToken);

        BrowseBookItemResponse[] items = result.Items
            .Select(item =>
                new BrowseBookItemResponse(
                    item.BookId,
                    item.Title,
                    item.FirstPublishedYear,
                    item.CoverImageUrl,
                    item.Authors
                        .Select(author =>
                            new BrowseBookAuthorResponse(
                                author.AuthorId,
                                author.Name))
                        .ToArray(),
                    item.Categories
                        .Select(categoryItem =>
                            new BrowseBookCategoryResponse(
                                categoryItem.CategoryId,
                                categoryItem.Name,
                                categoryItem.Slug))
                        .ToArray()))
            .ToArray();

        int totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                result.TotalCount /
                (double)result.PageSize);

        BrowseBooksResponse response = new(
            result.TotalCount,
            result.Page,
            result.PageSize,
            totalPages,
            items);

        return Results.Ok(response);
    }

    internal static Dictionary<string, string[]> Validate(
        string? query,
        string? searchField,
        string? category,
        int page,
        int pageSize,
        string? sort,
        CatalogBookSearchField? parsedSearchField,
        CatalogBookSort? parsedSort)
    {
        Dictionary<string, string[]> errors = [];

        string? normalizedQuery = query?.Trim();

        if (normalizedQuery?.Length > 200)
        {
            errors["query"] =
                ["Arama metni en fazla 200 karakter olabilir."];
        }

        if (!string.IsNullOrWhiteSpace(searchField) &&
            parsedSearchField is null)
        {
            errors["searchField"] =
            [
                "Arama alanı all, title veya author olmalıdır."
            ];
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            string normalizedCategory =
                category.Trim().ToLowerInvariant();

            bool containsInvalidCharacter =
                normalizedCategory.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character != '-');

            if (normalizedCategory.Length > 100 ||
                containsInvalidCharacter ||
                normalizedCategory.StartsWith('-') ||
                normalizedCategory.EndsWith('-') ||
                normalizedCategory.Contains(
                    "--",
                    StringComparison.Ordinal))
            {
                errors["category"] =
                [
                    "Kategori geçerli bir kategori slug değeri olmalıdır."
                ];
            }
        }

        if (page < 1)
        {
            errors["page"] =
                ["Sayfa numarası en az 1 olmalıdır."];
        }

        if (pageSize is < 1 or > 50)
        {
            errors["pageSize"] =
                ["Sayfa boyutu 1 ile 50 arasında olmalıdır."];
        }

        if (!string.IsNullOrWhiteSpace(sort) &&
            parsedSort is null)
        {
            errors["sort"] =
            [
                "Sıralama recently-added, title veya first-published-newest olmalıdır."
            ];
        }

        return errors;
    }

    private static CatalogBookSearchField? ParseSearchField(
        string? searchField)
    {
        string normalizedValue =
            string.IsNullOrWhiteSpace(searchField)
                ? "all"
                : searchField.Trim().ToLowerInvariant();

        return normalizedValue switch
        {
            "all" => CatalogBookSearchField.All,
            "title" => CatalogBookSearchField.Title,
            "author" => CatalogBookSearchField.Author,
            _ => null
        };
    }

    private static CatalogBookSort? ParseSort(
        string? sort)
    {
        string normalizedValue =
            string.IsNullOrWhiteSpace(sort)
                ? "recently-added"
                : sort.Trim().ToLowerInvariant();

        return normalizedValue switch
        {
            "recently-added" =>
                CatalogBookSort.RecentlyAdded,

            "recentlyadded" =>
                CatalogBookSort.RecentlyAdded,

            "title" =>
                CatalogBookSort.Title,

            "first-published-newest" =>
                CatalogBookSort.FirstPublishedNewest,

            "firstpublishednewest" =>
                CatalogBookSort.FirstPublishedNewest,

            _ => null
        };
    }
}