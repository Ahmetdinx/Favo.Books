using Favo.Books.Modules.Catalog.Domain.Categories;

namespace Favo.Books.Modules.Catalog.Application.Categories;

internal static class NovelGenreCatalog
{
    // General subjects (e.g. history) are not evidence for a novel subgenre.
    public static readonly string[] Slugs =
    [
        CatalogCategorySlugs.Classics, CatalogCategorySlugs.ScienceFiction,
        CatalogCategorySlugs.Fantasy, CatalogCategorySlugs.CrimeAndMystery,
        "tarihi-roman", "psikolojik-roman", "ask-ve-romantik", "macera", "gerilim", "korku", "distopya"
    ];
}
