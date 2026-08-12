namespace KitapKivo.Modules.Catalog.Presentation.Categories.GetAll;

public sealed record GetCategoriesResponse(
    int TotalCount,
    IReadOnlyList<GetCategoryItemResponse> Items);

public sealed record GetCategoryItemResponse(
    Guid CategoryId,
    string Name,
    string Slug,
    int BookCount);