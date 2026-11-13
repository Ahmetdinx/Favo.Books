using Favo.Books.Modules.Catalog.Presentation
    .Authors.Create;
using Favo.Books.Modules.Catalog.Presentation
    .Books.Browse;
using Favo.Books.Modules.Catalog.Presentation
    .Books.Create;
using Favo.Books.Modules.Catalog.Presentation
    .Books.GetById;
using Favo.Books.Modules.Catalog.Presentation
    .Books.GetExternalDetails;
using Favo.Books.Modules.Catalog.Presentation
    .Books.ResolveExternal;
using Favo.Books.Modules.Catalog.Presentation
    .Books.Search;
using Favo.Books.Modules.Catalog.Presentation
    .Categories.GetAll;
using Favo.Books.Modules.Catalog.Presentation
    .Imports.Create;
using Favo.Books.Modules.Catalog.Presentation
    .Imports.GetById;
using Favo.Books.Modules.Catalog.Presentation
    .Books.GetSummaries;

using Favo.Books.Modules.Catalog.Presentation.Books.Cover;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder
        MapCatalogEndpoints(
            this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/catalog")
            .WithTags("Catalog");

        group.MapCreateAuthorEndpoint();
        group.MapCreateBookEndpoint();
        group.MapSearchBooksEndpoint();
        group.MapGetBookByIdEndpoint();
        group.MapGetExternalBookDetailsEndpoint();
        group.MapResolveExternalBookEndpoint();
        group.MapCreateCatalogImportJobEndpoint();
        group.MapGetCatalogImportJobByIdEndpoint();
        group.MapBrowseBooksEndpoint();
        group.MapGetBookSummariesEndpoint();
        group.MapGetBookCoverImageEndpoint();
        group.MapGetCategoriesEndpoint();


        return app;
    }
}