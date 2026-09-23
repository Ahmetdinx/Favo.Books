using KitapKivo.Modules.Catalog.Application.Books;

namespace KitapKivo.Modules.Catalog.UnitTests.Domain.Books;

public sealed class PublisherNovelValidationTests
{
    [Theory]
    [InlineData("Z-")]
    [InlineData("O")]
    public void RealSingleLetterTitlesAreAccepted(string title)
        => ImportPublisherNovels.Validate(Entry(title));

    [Theory]
    [InlineData("---")]
    [InlineData("(Ciltli)")]
    public void TitlesWithoutBibliographicIdentityAreRejected(string title)
        => Assert.Throws<ArgumentException>(() => ImportPublisherNovels.Validate(Entry(title)));

    [Theory]
    [InlineData("Moby Dick ya da Balina – Sert Kapak", "Moby Dick")]
    [InlineData("Bin Dokuz Yüz Seksen Dört", "1984")]
    [InlineData("Nineteen Eighty Four", "1984")]
    [InlineData("Eroinle Dans- Cep", "Eroinle Dans")]
    [InlineData("Piraye Cep Kitabı", "Piraye")]
    [InlineData("1Q84 - 3. Kitap", "1Q84")]
    [InlineData("Savaş ve Barış 2 Cilt", "Savaş ve Barış")]
    [InlineData("3.900.- TL", "9.90")]
    [InlineData("4.900.- TL", "99 Francs")]
    public void KnownAlternateTitlesShareIdentity(string title, string canonical)
        => Assert.Equal(ImportPublisherNovels.TitleKey(canonical), ImportPublisherNovels.TitleKey(title));

    [Fact]
    public void PocketEditionSuffixDoesNotStripPartOfAName()
        => Assert.Equal("recep", ImportPublisherNovels.TitleKey("Recep"));

    private static PublisherNovel Entry(string title) => new(title, ["Fixture Author"], "9789944880985",
        "https://www.dogankitap.com.tr/files/kitaplar/img/fixture.jpg",
        "https://www.dogankitap.com.tr/kitap/fixture", "Romanlar", "Doğan Kitap", "tur", "und",
        200, 2020, 100, [], new string('a', 64), "https://www.dogankitap.com.tr/kitaplarimiz/romanlar/1");
}
