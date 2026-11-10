using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favo.Books.Modules.Catalog.Domain.Categories;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Persistence;
using Favo.Books.Modules.Community.Application;
using Favo.Books.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Profiles;

public sealed class ReaderCompatibilityTests(FavoBooksWebApplicationFactory factory) : IClassFixture<FavoBooksWebApplicationFactory>
{
    static readonly DiscoveryCategory Psychological = new("psikolojik-roman", "Psikolojik Roman");
    static readonly DiscoveryCategory Historical = new("tarihi-roman", "Tarihî Roman");
    static readonly DiscoveryCategory Horror = new("korku", "Korku");

    static PublicReaderRating Rated(Guid book, decimal overall) => new(book, 8, 8, 8, 8, 8, 8, 8, overall);
    static Guid[] Ids(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void Needs_both_readers_to_have_a_taste()
    {
        var books = Ids(5);
        var genres = books.ToDictionary(id => id, _ => new[] { Psychological });
        var result = ReaderCompatibility.Build(new(books, []), new(books[..2], []), [], new HashSet<Guid>(), genres);
        Assert.False(result.Ready);
        Assert.Null(result.Percent);
    }

    [Fact]
    public void Same_genres_and_scores_make_twins_and_suggest_their_loved_unread_books()
    {
        var shared = Ids(3);
        var theirOnly = Ids(4);
        var genres = shared.Concat(theirOnly).ToDictionary(id => id, _ => new[] { Psychological });
        var mine = new TasteSide([], shared.Select(id => Rated(id, 9)).ToArray());
        // Loved, loved, mediocre, and one already on my shelf.
        var theirs = new TasteSide([], shared.Select(id => Rated(id, 9))
            .Append(Rated(theirOnly[0], 9.5m)).Append(Rated(theirOnly[1], 8.4m)).Append(Rated(theirOnly[2], 5)).Append(Rated(theirOnly[3], 10)).ToArray());

        var result = ReaderCompatibility.Build(mine, theirs, [theirOnly[1]], new HashSet<Guid> { theirOnly[3] }, genres);

        Assert.True(result.Ready);
        Assert.Equal(100, result.Percent);
        Assert.Equal("Kitap ikizi", result.Label);
        Assert.Equal(["Psikolojik Roman"], result.SharedGenres);
        Assert.Equal(3, result.CommonBooks);
        Assert.Equal(3, result.ComparedRatings);
        // Influential first, then by score; mediocre and already-met books never appear.
        Assert.Equal([theirOnly[1], theirOnly[0]], result.Suggestions.Select(s => s.BookId).ToArray());
        Assert.True(result.Suggestions[0].Influential);
    }

    [Fact]
    public void Different_genres_and_opposite_scores_keep_readers_apart()
    {
        var mineBooks = Ids(5);
        var theirBooks = Ids(5);
        var both = Ids(2);
        var genres = mineBooks.ToDictionary(id => id, _ => new[] { Psychological });
        foreach (var id in theirBooks) genres[id] = [Horror];
        foreach (var id in both) genres[id] = [Historical];

        var mine = new TasteSide(mineBooks, both.Select(id => Rated(id, 10)).ToArray());
        var theirs = new TasteSide(theirBooks, both.Select(id => Rated(id, 1)).ToArray());
        var result = ReaderCompatibility.Build(mine, theirs, [], new HashSet<Guid>(), genres);

        Assert.True(result.Ready);
        Assert.True(result.Percent < 30);
        Assert.Equal("Farklı dünyalar", result.Label);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task Endpoint_compares_signed_in_reader_with_public_profiles_only()
    {
        var books = new List<Guid>();
        for (var i = 0; i < 4; i++) books.Add(await Novel());
        var (ayse, ayseName) = await Reader();
        foreach (var book in books)
            (await ayse.PutAsJsonAsync($"/api/community/books/{book}/rating", Scores(9))).EnsureSuccessStatusCode();
        var (me, _) = await Reader();
        foreach (var book in books.Take(3))
            (await me.PutAsJsonAsync($"/api/community/books/{book}/rating", Scores(9))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NotFound, (await me.GetAsync($"/api/readers/{ayseName}/compatibility")).StatusCode);
        (await ayse.PutAsJsonAsync("/api/readers/me", new { Nickname = ayseName, IsPublic = true })).EnsureSuccessStatusCode();

        var result = await me.GetFromJsonAsync<JsonElement>($"/api/readers/{ayseName}/compatibility");
        Assert.True(result.GetProperty("ready").GetBoolean());
        Assert.Equal(100, result.GetProperty("percent").GetInt32());
        Assert.Equal(books[3], result.GetProperty("suggestions")[0].GetProperty("bookId").GetGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/readers/{ayseName}/compatibility")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ayse.GetAsync($"/api/readers/{ayseName}/compatibility")).StatusCode);
    }

    static object Scores(int value) => new
    {
        Theme = value, Language = value, Characters = value, Structure = value, Pace = value, Impact = value, Experience = value
    };

    private async Task<(HttpClient Client, string Nickname)> Reader()
    {
        var client = factory.CreateClient();
        var name = "okur_" + Guid.NewGuid().ToString("N")[..16];
        var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = name };
        (await client.PostAsJsonAsync("/api/identity/register", credentials)).EnsureSuccessStatusCode();
        var login = await (await client.PostAsJsonAsync("/api/identity/login", credentials)).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        return (client, name);
    }

    private async Task<Guid> Novel()
    {
        var book = await CatalogTestData.CreateBookAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Editions.Add(new Edition(book, "Test Roman", "tur", EditionFormat.Paperback, coverImageUrl: "https://example.com/cover.jpg"));
        var genre = await db.Categories.SingleAsync(c => c.Slug == "psikolojik-roman");
        db.BookCategories.Add(new BookCategory(book, genre.Id));
        await db.SaveChangesAsync();
        return book;
    }
}
