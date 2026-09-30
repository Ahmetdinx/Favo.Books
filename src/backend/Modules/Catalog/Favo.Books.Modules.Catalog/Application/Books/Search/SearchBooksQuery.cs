namespace Favo.Books.Modules.Catalog.Application.Books.Search;

public sealed record SearchBooksQuery(
    string Query,
    int Page,
    int PageSize);