using KitapKivo.Modules.Catalog.Application.Categories.GetAll;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation.Categories.GetAll;

internal static class GetCategoriesEndpoint
{
    public static RouteGroupBuilder MapGetCategoriesEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapGet("/categories", HandleAsync)
            .WithName("GetCatalogCategories")
            .Produces<GetCategoriesResponse>(
                StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        GetCategoriesHandler handler,
        CancellationToken cancellationToken)
    {
        GetCategoriesQuery query = new();

        GetCategoriesResult result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        GetCategoryItemResponse[] items = result.Items
            .Select(category =>
                new GetCategoryItemResponse(
                    category.CategoryId,
                    category.Name,
                    category.Slug,
                    category.BookCount))
            .ToArray();

        GetCategoriesResponse response = new(
            result.TotalCount,
            items);

        return Results.Ok(response);
    }
}