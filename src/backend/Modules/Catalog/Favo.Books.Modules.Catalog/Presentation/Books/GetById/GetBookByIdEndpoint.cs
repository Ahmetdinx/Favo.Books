using Favo.Books.Modules.Catalog.Application.Books.GetById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog.Presentation.Books.GetById;

internal static class GetBookByIdEndpoint
{
    public static RouteGroupBuilder MapGetBookByIdEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapGet("/books/{bookId:guid}", HandleAsync)
            .WithName("GetBookById")
            .Produces<GetBookByIdResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid bookId,
        GetBookByIdHandler handler,
        CancellationToken cancellationToken)
    {
        GetBookByIdQuery query = new(bookId);

        GetBookByIdResult? result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        GetBookAuthorResponse[] authors = result.Authors
            .Select(author =>
                new GetBookAuthorResponse(
                    author.AuthorId,
                    author.Name,
                    author.DisplayOrder))
            .ToArray();

        GetBookCategoryResponse[] categories =
            result.Categories
                .Select(category =>
                    new GetBookCategoryResponse(
                        category.CategoryId,
                        category.Name,
                        category.Slug))
                .ToArray();

        GetBookEditionResponse[] editions = result.Editions
            .Select(edition =>
            {
                GetBookPublisherResponse? publisher =
                    edition.Publisher is null
                        ? null
                        : new GetBookPublisherResponse(
                            edition.Publisher.PublisherId,
                            edition.Publisher.Name);

                return new GetBookEditionResponse(
                    edition.EditionId,
                    edition.Title,
                    edition.LanguageCode,
                    edition.Format,
                    edition.Isbn10,
                    edition.Isbn13,
                    edition.PageCount,
                    edition.PublicationYear,
                    edition.CoverImageUrl,
                    publisher);
            })
            .ToArray();

        GetBookByIdResponse response = new(
            result.BookId,
            result.Title,
            result.OriginalLanguageCode,
            result.OriginalTitle,
            result.Description,
            result.FirstPublishedYear,
            result.PrimaryEditionId,
            result.PrimaryCoverImageUrl,
            authors,
            categories,
            editions);

        return Results.Ok(response);
    }
}