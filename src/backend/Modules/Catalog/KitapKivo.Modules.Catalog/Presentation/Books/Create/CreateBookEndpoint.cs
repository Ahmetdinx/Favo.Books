using KitapKivo.Modules.Catalog.Application.Books.Create;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation.Books.Create;

internal static class CreateBookEndpoint
{
    public static RouteGroupBuilder MapCreateBookEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapPost("/books", HandleAsync)
            .WithName("CreateBook")
            .RequireAuthorization()
            .Produces<CreateBookResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateBookRequest request,
        CreateBookHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        CreateBookCommand command = new(
            request.Title!.Trim(),
            request.OriginalLanguageCode!.Trim(),
            request.OriginalTitle,
            request.Description,
            request.FirstPublishedYear,
            request.AuthorIds!);

        CreateBookResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.RejectionReason is not null)
                return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Doğrulanmış roman kaydı gerekiyor.",
                    detail: "Serbest kitap ekleme kapalıdır. Kaynakla doğrulanmış roman ekleme akışını kullanın.",
                    extensions: new Dictionary<string, object?> { ["code"] = result.RejectionReason });
            string missingAuthorIds = string.Join(
                ", ",
                result.MissingAuthorIds);

            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["authorIds"] =
                    [
                        $"Şu yazar kimlikleri bulunamadı: {missingAuthorIds}"
                    ]
                });
        }

        Guid bookId = result.BookId
            ?? throw new InvalidOperationException(
                "Book identifier was not returned.");

        CreateBookResponse response = new(bookId);

        return Results.Created(
            $"/api/catalog/books/{bookId}",
            response);
    }

    private static Dictionary<string, string[]> Validate(
        CreateBookRequest request)
    {
        Dictionary<string, string[]> errors = [];

        string? title = request.Title?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] =
                ["Kitap adı zorunludur."];
        }
        else if (title.Length > 500)
        {
            errors["title"] =
                ["Kitap adı en fazla 500 karakter olabilir."];
        }

        string? languageCode =
            request.OriginalLanguageCode?.Trim();

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            errors["originalLanguageCode"] =
                ["Orijinal dil kodu zorunludur."];
        }
        else if (languageCode.Length > 10)
        {
            errors["originalLanguageCode"] =
                ["Dil kodu en fazla 10 karakter olabilir."];
        }

        if (request.OriginalTitle?.Trim().Length > 500)
        {
            errors["originalTitle"] =
                ["Orijinal kitap adı en fazla 500 karakter olabilir."];
        }

        if (request.FirstPublishedYear is < 0 or > 9999)
        {
            errors["firstPublishedYear"] =
                ["İlk yayımlanma yılı 0 ile 9999 arasında olmalıdır."];
        }

        if (request.AuthorIds is null ||
            request.AuthorIds.Length == 0)
        {
            errors["authorIds"] =
                ["En az bir yazar seçilmelidir."];
        }
        else if (request.AuthorIds.Any(
                     authorId => authorId == Guid.Empty))
        {
            errors["authorIds"] =
                ["Yazar kimliği boş olamaz."];
        }
        else if (request.AuthorIds.Distinct().Count() !=
                 request.AuthorIds.Length)
        {
            errors["authorIds"] =
                ["Aynı yazar birden fazla kez eklenemez."];
        }

        return errors;
    }
}
