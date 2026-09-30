using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class ReviewedReadingListTests(FavoBooksWebApplicationFactory factory)
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    [Fact]
    public async Task PreviewDoesNotWrite_ImportPreservesIdentity_ExceptionsAreVisible_ReplayIsIdempotent()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var batch = "reading-list-test-" + Guid.NewGuid();
        var e = Entry(1,"9780000000309");
        var before = await db.Books.CountAsync();
        var preview = await new ImportReviewedReadingList(db).ExecuteAsync([e], batch, false);
        Assert.Single(preview); Assert.Equal(before, await db.Books.CountAsync());
        db.ChangeTracker.Clear();
        var imported = await new ImportReviewedReadingList(db).ExecuteAsync([e], batch, true);
        Assert.True(imported[0].Visible);
        var id = imported[0].BookId;
        db.ChangeTracker.Clear();
        var replay = await new ImportReviewedReadingList(db).ExecuteAsync([e], batch, true);
        Assert.Equal(id, replay[0].BookId); Assert.False(replay[0].Created);
        db.ChangeTracker.Clear();
        var edit = e with { Title = "Türkçe " + e.Title, ExistingBookId = id,
            ExpectedTitle = e.Title, ExpectedAuthor = e.Author, Decision = NovelDecision.SelectedClassic,
            Isbn13 = "9780000000354", SourceUrl = e.SourceUrl.Replace(".html", "-new-edition.html") };
        var updated = await new ImportReviewedReadingList(db).ExecuteAsync([edit], batch + "-review", true);
        Assert.Equal(id, updated[0].BookId); Assert.True(updated[0].Visible);
        Assert.Equal(before+1, await db.Books.CountAsync());
        Assert.Equal(e.Title, (await db.Books.SingleAsync(b => b.Id == id)).OriginalTitle);
        Assert.Equal(2, await db.Editions.CountAsync(x => x.BookId == id));
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportReviewedReadingList(db)
            .ExecuteAsync([e with { Title = "changed" }], batch, true));
    }

    [Fact]
    public async Task SameTitleDifferentAuthorIsDistinct_IsbnCollisionRollsBackEntireList()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var batch = "reading-list-collision-"+Guid.NewGuid();
        var first = Entry(1,"9780000000316");
        await new ImportReviewedReadingList(db).ExecuteAsync([first],batch,true);
        db.ChangeTracker.Clear();
        var second = Entry(2,"9780000000323") with { Title=first.Title, Author="Different Author" };
        var result = await new ImportReviewedReadingList(db).ExecuteAsync([second],batch+"-different",true);
        Assert.True(result[0].Created);
        db.ChangeTracker.Clear();
        var before = await db.Books.CountAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ImportReviewedReadingList(db).ExecuteAsync(
            [Entry(3,"9780000000330"),Entry(4,first.Isbn13!)],batch+"-bad",true));
        Assert.Equal(before, await db.Books.CountAsync());
    }

    [Fact]
    public void RejectsForeignEditionsUnknownFormsAndUntrustedCovers()
    {
        var e=Entry(1,"9780000000347");
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { LanguageCode="eng" }));
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { Decision=NovelDecision.Unknown }));
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { CoverImageUrl="https://localhost/image" }));
    }

    [Fact]
    public void VintageEvidenceAcceptsVerifiedIsbnOrDocumentedAbsence()
    {
        var e = Entry(1,"9780000000347") with {
            SourceUrl="https://www.nadirkitap.com/fixture-kitap1.html",
            CoverImageUrl="https://static.nadirkitap.com/fixture.jpg"
        };
        ImportReviewedReadingList.Validate(e);
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { Isbn13=null }));
        ImportReviewedReadingList.Validate(e with { Isbn13=null, EvidenceNote="ISBN unavailable in source; checked edition." });
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { Isbn13="9780000000348" }));
        Assert.Throws<ArgumentException>(()=>ImportReviewedReadingList.Validate(e with { CoverImageUrl="https://example.com/cover.jpg" }));
    }

    private static ReviewedReadingListEntry Entry(int n,string isbn) => new(n,"Reviewed "+Guid.NewGuid(),"Fixture Author",
        "Fixture edition",isbn,"https://img.kitapyurdu.com/v1/getImage/fn:123/wi:200","Fixture Publisher",100,
        "https://www.kitapyurdu.com/kitap/fixture/"+Guid.NewGuid()+".html",new string('a',64),"tur",
        "Explicitly reviewed fixture",NovelDecision.Novel);
}
