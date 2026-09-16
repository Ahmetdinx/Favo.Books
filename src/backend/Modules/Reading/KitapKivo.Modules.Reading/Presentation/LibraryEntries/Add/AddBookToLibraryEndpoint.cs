using System.Security.Claims;

using KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;
using KitapKivo.Modules.Reading
    .Domain.LibraryEntries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.Add;

internal static class AddBookToLibraryEndpoint
{
    public static RouteGroupBuilder MapAddBookToLibraryEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapPost("/library", HandleAsync)
            .WithName("AddBookToLibrary")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<AddBookToLibraryResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        AddBookToLibraryRequest request,
        ClaimsPrincipal user,
        AddBookToLibraryHandler handler,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Results.Unauthorized();
        }

        Dictionary<string, string[]> errors = [];

        if (request.BookId == Guid.Empty)
        {
            errors["bookId"] =
                ["Kitap kimliği boş olamaz."];
        }

        if (!TryParseStatus(
                request.Status,
                out LibraryEntryStatus status))
        {
            errors["status"] =
            [
                "Durum WantToRead, Reading, Read, " +
                "OnHold veya Dropped olmalıdır."
            ];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        AddBookToLibraryCommand command = new(
            userId,
            request.BookId,
            status);

        AddBookToLibraryResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (result.Error == AddBookToLibraryError.BookNotFound)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kitap bulunamadı.",
                detail: "Seçilen kitap katalogda bulunamadı.");
        }

        if (!result.IsSuccess)
        {
            return Results.Conflict(new
            {
                message =
                    "Bu kitap kullanıcının kütüphanesinde zaten bulunuyor."
            });
        }

        Guid libraryEntryId =
            result.LibraryEntryId
            ?? throw new InvalidOperationException(
                "Library entry identifier was not returned.");

        AddBookToLibraryResponse response =
            new(libraryEntryId);

        return Results.Created(
            $"/api/reading/library/{libraryEntryId}",
            response);
    }

    private static bool TryParseStatus(
        string? value,
        out LibraryEntryStatus status)
    {
        status = default;

        string? normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue) ||
            int.TryParse(normalizedValue, out _))
        {
            return false;
        }

        return Enum.TryParse(
                   normalizedValue,
                   ignoreCase: true,
                   out status) &&
               Enum.IsDefined(status);
    }
}
