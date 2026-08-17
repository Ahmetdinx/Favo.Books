using System.Text.Json;

using KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ImportExternal;

internal static class ImportExternalBooksEndpoint
{
    private const string SupportedProvider =
        "OpenLibrary";

    private const int MaximumBatchSize = 20;

    private const int MaximumQueryLength = 200;

    public static RouteGroupBuilder
        MapImportExternalBooksEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost(
                "/books/external/import",
                HandleAsync)
            .WithName("ImportExternalBooks")
            .RequireAuthorization()
            .Produces<ImportExternalBooksResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .ProducesProblem(
                StatusCodes.Status502BadGateway)
            .ProducesProblem(
                StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(
                StatusCodes.Status504GatewayTimeout);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        ImportExternalBooksRequest request,
        ImportExternalBooksHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(
                request.Provider,
                request.Query,
                request.Page,
                request.PageSize);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        ImportExternalBooksCommand command = new(
            request.Provider!.Trim(),
            request.Query!.Trim(),
            request.Page,
            request.PageSize);

        ImportExternalBooksResult result;

        try
        {
            result = await handler.HandleAsync(
                command,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken
                .IsCancellationRequested)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status504GatewayTimeout,
                title:
                    "OpenLibrary isteği zaman aşımına uğradı.",
                detail:
                    "OpenLibrary belirlenen süre içinde " +
                    "arama isteğine cevap vermedi.");
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status503ServiceUnavailable,
                title:
                    "OpenLibrary servisine ulaşılamıyor.",
                detail:
                    "Dış kitap sağlayıcısına yapılan " +
                    "arama isteği başarısız oldu.");
        }
        catch (JsonException)
        {
            return Results.Problem(
                statusCode:
                    StatusCodes.Status502BadGateway,
                title:
                    "OpenLibrary geçersiz cevap döndürdü.",
                detail:
                    "Dış kitap sağlayıcısının cevabı " +
                    "işlenemedi.");
        }

        ImportExternalBookFailureResponse[] failures =
            result.Failures
                .Select(failure =>
                    new ImportExternalBookFailureResponse(
                        failure.ExternalId,
                        failure.Title,
                        failure.Reason))
                .ToArray();

        ImportExternalBooksResponse response = new(
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.FoundCount,
            result.ProcessedCount,
            result.CreatedCount,
            result.ExistingCount,
            result.NotFoundCount,
            result.FailedCount,
            failures);

        return Results.Ok(response);
    }

    internal static Dictionary<string, string[]> Validate(
        string? provider,
        string? query,
        int page,
        int pageSize)
    {
        Dictionary<string, string[]> errors = [];

        string? normalizedProvider =
            provider?.Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedProvider))
        {
            errors["provider"] =
                ["Kitap sağlayıcısı zorunludur."];
        }
        else if (!string.Equals(
                     normalizedProvider,
                     SupportedProvider,
                     StringComparison.OrdinalIgnoreCase))
        {
            errors["provider"] =
            [
                "Yalnızca OpenLibrary sağlayıcısı " +
                "desteklenmektedir."
            ];
        }

        string? normalizedQuery =
            query?.Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedQuery))
        {
            errors["query"] =
                ["Arama sorgusu zorunludur."];
        }
        else if (normalizedQuery.Length >
                 MaximumQueryLength)
        {
            errors["query"] =
            [
                $"Arama sorgusu en fazla " +
                $"{MaximumQueryLength} karakter olabilir."
            ];
        }

        if (page < 1)
        {
            errors["page"] =
                ["Sayfa numarası sıfırdan büyük olmalıdır."];
        }

        if (pageSize is < 1 or > MaximumBatchSize)
        {
            errors["pageSize"] =
            [
                $"Sayfa boyutu 1 ile " +
                $"{MaximumBatchSize} arasında olmalıdır."
            ];
        }

        return errors;
    }
}