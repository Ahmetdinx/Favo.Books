using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Add;
using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Get;
using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.GetByBook;
using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.GetSummary;
using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Remove;
using Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.UpdateStatus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Favo.Books.Modules.Reading.Presentation.LibraryEntries;

namespace Favo.Books.Modules.Reading;

public static class ReadingEndpoints
{
    public static IEndpointRouteBuilder
        MapReadingEndpoints(
            this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/reading")
            .WithTags("Reading");

        group.MapAddBookToLibraryEndpoint();
        group.MapGetLibraryEndpoint();
        group.MapGetLibrarySummaryEndpoint();
        group.MapGetLibraryEntryByBookEndpoint();
        group.MapLookupLibraryEntries();
        group.MapUndoLibraryChange();
        group.MapUpdateLibraryStatusEndpoint();
        group.MapRemoveBookFromLibraryEndpoint();

        return app;
    }
}
