using Favo.Books.Modules.Community.Presentation.Ratings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Community;

public static class CommunityEndpoints
{
    public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/community").WithTags("Community");
        group.MapBookRatingEndpoints();
        group.MapRecommendationEndpoints();
        return app;
    }
}
