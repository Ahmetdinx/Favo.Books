namespace Favo.Books.Modules.Catalog.Application.Categories.GetAll;

public sealed record GetCategoriesResult(
    int TotalCount,
    IReadOnlyList<GetCategoryItemResult> Items);

public sealed record GetCategoryItemResult(
    Guid CategoryId,
    string Name,
    string Slug,
    int BookCount);