using Favo.Books.Modules.Catalog.Application
    .Books.GetSummaries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog.Presentation
    .Books.GetSummaries;

internal static class GetBookSummariesEndpoint
{
    public static RouteGroupBuilder
        MapGetBookSummariesEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost(
                "/books/summaries",
                HandleAsync)
            .WithName("GetBookSummaries")
            .Produces<GetBookSummariesResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        GetBookSummariesRequest request,
        GetBookSummariesHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(request.BookIds);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        Guid[] distinctBookIds = request.BookIds!
            .Distinct()
            .ToArray();

        GetBookSummariesQuery query = new(
            distinctBookIds);

        GetBookSummariesResult result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        GetBookSummaryItemResponse[] items =
            result.Items
                .Select(item =>
                    new GetBookSummaryItemResponse(
                        item.BookId,
                        item.Title,
                        item.FirstPublishedYear,
                        item.CoverImageUrl,
                        item.Authors
                            .Select(author =>
                                new GetBookSummaryAuthorResponse(
                                    author.AuthorId,
                                    author.Name))
                            .ToArray(),
                        item.Categories
                            .Select(category =>
                                new GetBookSummaryCategoryResponse(
                                    category.CategoryId,
                                    category.Name,
                                    category.Slug))
                            .ToArray()))
                .ToArray();

        return Results.Ok(
            new GetBookSummariesResponse(items));
    }

    internal static Dictionary<string, string[]>
        Validate(
            IReadOnlyList<Guid>? bookIds)
    {
        Dictionary<string, string[]> errors = [];
        List<string> bookIdErrors = [];

        if (bookIds is null || bookIds.Count == 0)
        {
            bookIdErrors.Add(
                "En az bir kitap kimliği belirtilmelidir.");
        }
        else
        {
            if (bookIds.Count > 100)
            {
                bookIdErrors.Add(
                    "Bir istekte en fazla 100 kitap " +
                    "kimliği gönderilebilir.");
            }

            if (bookIds.Any(bookId =>
                    bookId == Guid.Empty))
            {
                bookIdErrors.Add(
                    "Kitap kimlikleri boş olamaz.");
            }
        }

        if (bookIdErrors.Count > 0)
        {
            errors["bookIds"] =
                bookIdErrors.ToArray();
        }

        return errors;
    }
}