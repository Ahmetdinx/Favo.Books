using KitapKivo.Modules.Catalog.Application
    .Categories.Mapping;
using KitapKivo.Modules.Catalog.Domain.Categories;

namespace KitapKivo.Modules.Catalog.UnitTests
    .Application.Categories.Mapping;

public sealed class SubjectCategoryMapperTests
{
    [Theory]
    [InlineData("Historical fiction", "tarihi-roman")]
    [InlineData("Psikolojik roman", "psikolojik-roman")]
    [InlineData("Romance novel", "ask-ve-romantik")]
    [InlineData("Adventure fiction", "macera")]
    [InlineData("Thriller", "gerilim")]
    [InlineData("Horror fiction", "korku")]
    [InlineData("Dystopian fiction", "distopya")]
    public void Map_RecognizesExplicitNovelGenres(string subject, string slug)
        => Assert.Contains(slug, SubjectCategoryMapper.Map([subject]));

    [Fact]
    public void Map_DoesNotTurnGeneralSubjectsIntoNovelGenres()
    {
        var result = SubjectCategoryMapper.Map(["History", "Psychology", "Tarih", "Psikoloji"]);
        Assert.DoesNotContain("tarihi-roman", result);
        Assert.DoesNotContain("psikolojik-roman", result);
    }

    [Fact]
    public void Map_ShouldReturnMatchingCategories()
    {
        // Arrange
        string[] subjects =
        [
            "Science Fiction",
            "Fantasy fiction",
            "Children's stories"
        ];

        // Act
        IReadOnlyList<string> result =
            SubjectCategoryMapper.Map(subjects);

        // Assert
        Assert.Equal(
            [
                CatalogCategorySlugs.ScienceFiction,
                CatalogCategorySlugs.Children,
                CatalogCategorySlugs.Fantasy,
                CatalogCategorySlugs.Novel
            ],
            result);
    }

    [Fact]
    public void Map_ShouldReturnCategoryOnlyOnce()
    {
        // Arrange
        string[] subjects =
        [
            "Detective stories",
            "Crime",
            "Mystery"
        ];

        // Act
        IReadOnlyList<string> result =
            SubjectCategoryMapper.Map(subjects);

        // Assert
        Assert.Equal(
            [
                CatalogCategorySlugs.CrimeAndMystery
            ],
            result);
    }

    [Fact]
    public void Map_ShouldSupportTurkishSubjects()
    {
        // Arrange
        string[] subjects =
        [
            "Şiir",
            "Kişisel Gelişim",
            "Çocuk Edebiyatı"
        ];

        // Act
        IReadOnlyList<string> result =
            SubjectCategoryMapper.Map(subjects);

        // Assert
        Assert.Equal(
            [
                CatalogCategorySlugs.Children,
                CatalogCategorySlugs.PersonalDevelopment,
                CatalogCategorySlugs.Poetry
            ],
            result);
    }

    [Fact]
    public void Map_ShouldIgnoreEmptyAndUnknownSubjects()
    {
        // Arrange
        string[] subjects =
        [
            "",
            "   ",
            "Unmapped Subject"
        ];

        // Act
        IReadOnlyList<string> result =
            SubjectCategoryMapper.Map(subjects);

        // Assert
        Assert.Empty(result);
    }
}
