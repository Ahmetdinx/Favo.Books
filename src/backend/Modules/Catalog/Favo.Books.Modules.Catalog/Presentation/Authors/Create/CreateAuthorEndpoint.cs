using Favo.Books.Modules.Catalog.Application.Authors.Create;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Favo.Books.SharedKernel.Authorization;

namespace Favo.Books.Modules.Catalog.Presentation.Authors.Create;

internal static class CreateAuthorEndpoint
{
    public static RouteGroupBuilder MapCreateAuthorEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapPost("/authors", HandleAsync)
            .WithName("CreateAuthor")
            .RequireAuthorization(AuthorizationPolicies.CatalogAdmin)
            .Produces<CreateAuthorResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateAuthorRequest request,
        CreateAuthorHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        CreateAuthorCommand command = new(
            request.Name!.Trim(),
            request.Biography,
            request.BirthDate,
            request.DeathDate);

        CreateAuthorResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        CreateAuthorResponse response =
            new(result.AuthorId);

        return Results.Created(
            $"/api/catalog/authors/{result.AuthorId}",
            response);
    }

    private static Dictionary<string, string[]> Validate(
        CreateAuthorRequest request)
    {
        Dictionary<string, string[]> errors = [];

        string? name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] =
                ["Yazar adı zorunludur."];
        }
        else if (name.Length > 300)
        {
            errors["name"] =
                ["Yazar adı en fazla 300 karakter olabilir."];
        }

        if (request.BirthDate.HasValue &&
            request.DeathDate.HasValue &&
            request.DeathDate.Value <
            request.BirthDate.Value)
        {
            errors["deathDate"] =
            [
                "Ölüm tarihi doğum tarihinden önce olamaz."
            ];
        }

        return errors;
    }
}