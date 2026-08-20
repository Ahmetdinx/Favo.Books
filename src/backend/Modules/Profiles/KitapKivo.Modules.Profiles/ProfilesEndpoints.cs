using KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Get;
using KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Remove;
using KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Set;
using KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Get;
using KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Remove;
using KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Set;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Profiles;

public static class ProfilesEndpoints
{
    public static IEndpointRouteBuilder
        MapProfilesEndpoints(
            this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/profiles")
            .WithTags("Profiles");

        group.MapSetBedsideBookEndpoint();
        group.MapGetBedsideBookEndpoint();
        group.MapRemoveBedsideBookEndpoint();

        group.MapSetInfluentialBookEndpoint();
        group.MapGetInfluentialBooksEndpoint();
        group.MapRemoveInfluentialBookEndpoint();

        return app;
    }
}