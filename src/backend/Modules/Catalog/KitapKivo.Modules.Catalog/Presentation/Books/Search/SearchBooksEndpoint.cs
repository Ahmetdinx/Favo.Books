using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application.Books.Search;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation.Books.Search;

internal static class SearchBooksEndpoint
{
    public static RouteGroupBuilder MapSearchBooksEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapGet("/books/search", HandleAsync)
            .WithName("SearchBooks")
            .Produces<SearchBooksResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string? query,
        int? page,
        int? pageSize,
        SearchBooksHandler handler,
        CancellationToken cancellationToken)
    {
        int requestedPage = page ?? 1;
        int requestedPageSize = pageSize ?? 20;

        Dictionary<string, string[]> errors = Validate(
            query,
            requestedPage,
            requestedPageSize);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        SearchBooksQuery searchQuery = new(
            query!.Trim(),
            requestedPage,
            requestedPageSize);

        ExternalBookSearchResult result =
            await handler.HandleAsync(
                searchQuery,
                cancellationToken);

        SearchBookItemResponse[] items = result.Items
            .Select(item =>
                new SearchBookItemResponse(
                    item.Provider,
                    item.ExternalId,
                    item.Title,
                    item.Authors
                        .Select(author =>
                            new SearchBookAuthorResponse(
                                author.ExternalId,
                                author.Name))
                        .ToArray(),
                    item.FirstPublishedYear,
                    item.CoverUrl,
                    item.Isbns.ToArray(),
                    item.LanguageCodes.ToArray()))
            .ToArray();

        SearchBooksResponse response = new(
            result.TotalCount,
            result.Page,
            result.PageSize,
            items);

        return Results.Ok(response);
    }

    internal static Dictionary<string, string[]> Validate(
        string? query,
        int page,
        int pageSize)
    {
        Dictionary<string, string[]> errors = [];

        string? normalizedQuery = query?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            errors["query"] =
                ["Arama metni zorunludur."];
        }
        else if (normalizedQuery.Length > 200)
        {
            errors["query"] =
                ["Arama metni en fazla 200 karakter olabilir."];
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

        return errors;
    }
}