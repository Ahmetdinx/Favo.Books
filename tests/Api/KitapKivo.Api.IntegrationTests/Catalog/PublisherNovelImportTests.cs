using KitapKivo.Modules.Catalog.Application.Books;
using KitapKivo.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class PublisherNovelImportTests(KitapKivoWebApplicationFactory factory)
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    [Fact]
    public async Task PreviewIsReadOnly_ExactTargetIsAtomic_RepeatDoesNotAddAnotherBatch()
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var handler=new ImportPublisherNovels(db);
        var first=Entry("9786053322122"); var second=Entry("9789750741388");
        var batch="publisher-test-"+Guid.NewGuid().ToString("N");
        var before=await db.Books.CountAsync();
        var preview=await handler.ExecuteAsync([first,second],1,batch,false);
        Assert.Equal(2,preview.AvailableNew); Assert.Single(preview.Items);
        Assert.Equal(before,await db.Books.CountAsync());
        var applied=await handler.ExecuteAsync([first,second],1,batch,true);
        Assert.True(applied.Applied); Assert.Single(applied.Items);
        Assert.Equal(before+1,await db.Books.CountAsync());
        Assert.True(await db.Books.EligibleNovels().AnyAsync(b=>b.Id==applied.Items[0].BookId));
        var repeat=await handler.ExecuteAsync([first,second],1,batch,true);
        Assert.False(repeat.Applied); Assert.Equal(applied.Items[0].BookId,repeat.Items[0].BookId);
        Assert.Equal(before+1,await db.Books.CountAsync());
    }

    [Fact]
    public async Task DifferentEditionOfSameTitleIsNotCountedAsNew_InsufficientPoolDoesNotWrite()
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var handler=new ImportPublisherNovels(db);
        var first=Entry("9789750712838");
        var second=Entry("9789759099541") with { Title=first.Title+" – Sert Kapak" };
        var batch="duplicate-test-"+Guid.NewGuid().ToString("N");
        var before=await db.Books.CountAsync();
        var preview=await handler.ExecuteAsync([first,second],2,batch,false);
        Assert.Equal(1,preview.AvailableNew); Assert.Equal(1,preview.ExistingOrDuplicate);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>handler.ExecuteAsync([first,second],2,batch,true));
        Assert.Equal(before,await db.Books.CountAsync());
    }

    [Fact]
    public async Task BadEntryRejectsWholeBatchBeforeWrites()
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var valid=Entry("9789944880985");
        var before=await db.Books.CountAsync();
        await Assert.ThrowsAsync<ArgumentException>(()=>new ImportPublisherNovels(db).ExecuteAsync(
            [valid,valid with { Isbn13="9789944880986" }],1,"invalid-"+Guid.NewGuid(),true));
        Assert.Equal(before,await db.Books.CountAsync());
    }

    [Fact]
    public async Task KnownAlternateTitleDoesNotCreateAnotherReviewedWork()
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var handler=new ImportPublisherNovels(db);
        var original=Entry("9780000000019") with { Title="Moby Dick" };
        await handler.ExecuteAsync([original],1,"alias-original-"+Guid.NewGuid(),true);
        var alternate=Entry("9780000000026") with { Title="Moby Dick ya da Balina – Ciltli" };
        var result=await handler.ExecuteAsync([alternate],1,"alias-preview-"+Guid.NewGuid(),false);
        Assert.Equal(0,result.AvailableNew); Assert.Equal(1,result.ExistingOrDuplicate);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void RejectsUntrustedSourcesAndMissingTurkishEditionEvidence()
    {
        var e=Entry("9789944880985");
        Assert.Throws<ArgumentException>(()=>ImportPublisherNovels.Validate(e with { SourceCategory="Öykü" }));
        Assert.Throws<ArgumentException>(()=>ImportPublisherNovels.Validate(e with { LanguageCode="eng" }));
        Assert.Throws<ArgumentException>(()=>ImportPublisherNovels.Validate(e with { SourceUrl="https://unknown.example/kitap/test" }));
        Assert.Throws<ArgumentException>(()=>ImportPublisherNovels.Validate(e with { CoverImageUrl="https://localhost/cover.jpg" }));
        Assert.Throws<ArgumentException>(()=>ImportPublisherNovels.Validate(e with { SourceSha256="" }));
    }

    private static PublisherNovel Entry(string isbn)
    {
        var unique=Guid.NewGuid().ToString("N");
        return new("Publisher fixture "+unique,["Fixture Author"],isbn,
            "https://www.dogankitap.com.tr/files/kitaplar/img/fixture.jpg",
            "https://www.dogankitap.com.tr/kitap/"+unique,"Romanlar","Doğan Kitap","tur","und",
            200,2020,100,[],new string('a',64),"https://www.dogankitap.com.tr/kitaplarimiz/romanlar/1");
    }
}
