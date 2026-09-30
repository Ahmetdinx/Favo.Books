using System.Security.Claims;

using Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetByBook;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.GetByBook;

internal static class GetLibraryEntryByBookEndpoint
{
    public static RouteGroupBuilder
        MapGetLibraryEntryByBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/library/books/{bookId:guid}",
                HandleAsync)
            .WithName("GetLibraryEntryByBook")
            .RequireAuthorization()
            .Produces<GetLibraryEntryByBookResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid bookId,
        ClaimsPrincipal user,
        GetLibraryEntryByBookHandler handler,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(
                userIdValue,
                out Guid userId))
        {
            return Results.Unauthorized();
        }

        if (bookId == Guid.Empty)
        {
            Dictionary<string, string[]> errors =
                new()
                {
                    ["bookId"] =
                        ["Kitap kimliği boş olamaz."]
                };

            return Results.ValidationProblem(errors);
        }

        GetLibraryEntryByBookQuery query = new(
            userId,
            bookId);

        GetLibraryEntryByBookResult? result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound(new
            {
                message =
                    "Kütüphane kaydı bulunamadı."
            });
        }

        GetLibraryEntryByBookResponse response =
            new(
                result.LibraryEntryId,
                result.BookId,
                result.Status.ToString(),
                result.CreatedAtUtc,
                result.UpdatedAtUtc);

        return Results.Ok(response);
    }
}