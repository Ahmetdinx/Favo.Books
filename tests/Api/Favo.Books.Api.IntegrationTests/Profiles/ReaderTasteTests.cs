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

public sealed class ReaderTasteTests(FavoBooksWebApplicationFactory factory) : IClassFixture<FavoBooksWebApplicationFactory>
{
    static readonly DiscoveryCategory Psychological = new("psikolojik-roman", "Psikolojik Roman");
    static readonly DiscoveryCategory Historical = new("tarihi-roman", "Tarihî Roman");
    static readonly DiscoveryCategory Classics = new("klasikler", "Klasikler");

    static PublicReaderRating Rating(Guid book, int theme, int language, int characters, int structure, int pace, int impact, int experience)
    {
        var overall = (theme * 15 + language * 15 + characters * 15 + structure * 15 + pace * 10 + impact * 15 + experience * 15) / 100m;
        return new(book, theme, language, characters, structure, pace, impact, experience, overall);
    }

    [Fact]
    public void Not_ready_until_five_books_or_three_ratings()
    {
        var a = Guid.NewGuid();
        var profile = ReaderTaste.Build([a], [], new Dictionary<Guid, DiscoveryCategory[]> { [a] = [Psychological] });
        Assert.False(profile.Ready);
        Assert.Equal(4, profile.BooksNeeded);
        Assert.Equal(3, profile.RatingsNeeded);
        Assert.Null(profile.Title);
        Assert.Empty(profile.Genres);
    }

    [Fact]
    public void Liked_books_drive_genres_strengths_and_traits()
    {
        var loved = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var disliked = Guid.NewGuid();
        var genres = loved.ToDictionary(id => id, _ => new[] { Psychological, Classics });
        genres[disliked] = [Historical];
        // Language and characters stand out in every loved book; pace is the weakest.
        var rated = loved.Select(id => Rating(id, 8, 10, 10, 8, 6, 8, 8))
            .Append(Rating(disliked, 3, 3, 3, 3, 3, 3, 3)).ToArray();

        var profile = ReaderTaste.Build([], rated, genres);

        Assert.True(profile.Ready);
        Assert.Equal("İç dünyaların okuru", profile.Title);
        Assert.Equal("psikolojik-roman", profile.Genres[0].Slug);
        Assert.True(profile.Genres[0].Percent > 80);
        Assert.DoesNotContain(profile.Genres, g => g.Slug == "klasikler");
        Assert.Equal(["characters", "language"], profile.Strengths.Select(s => s.Key).Order().ToArray());
        Assert.Contains("Klasiklere sadık", profile.Traits);
        Assert.Contains("Dengeli puanlayıcı", profile.Traits);
    }

    [Fact]
    public void Flat_scores_give_no_strengths()
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var profile = ReaderTaste.Build([], ids.Select(id => Rating(id, 9, 9, 9, 9, 9, 9, 9)).ToArray(),
            ids.ToDictionary(id => id, _ => new[] { Historical }));
        Assert.True(profile.Ready);
        Assert.Empty(profile.Strengths);
        Assert.Contains("Cömert puanlayıcı", profile.Traits);
        Assert.Equal("Zaman yolcusu", profile.Title);
    }

    [Fact]
    public async Task Endpoints_return_own_profile_and_public_profiles_only()
    {
        var client = factory.CreateClient();
        var name = "okur_" + Guid.NewGuid().ToString("N")[..16];
        var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = name };
        (await client.PostAsJsonAsync("/api/identity/register", credentials)).EnsureSuccessStatusCode();
        var login = await (await client.PostAsJsonAsync("/api/identity/login", credentials)).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());

        for (var i = 0; i < 3; i++)
        {
            var book = await Novel();
            (await client.PutAsJsonAsync($"/api/community/books/{book}/rating", new
            {
                Theme = 8, Language = 10, Characters = 9, Structure = 8, Pace = 6, Impact = 8, Experience = 8
            })).EnsureSuccessStatusCode();
        }

        var own = await client.GetFromJsonAsync<JsonElement>("/api/readers/me/taste");
        Assert.True(own.GetProperty("ready").GetBoolean());
        Assert.Equal(3, own.GetProperty("ratingCount").GetInt32());
        Assert.Equal("Distopya kâşifi", own.GetProperty("title").GetString());
        Assert.Equal("language", own.GetProperty("strengths")[0].GetProperty("key").GetString());

        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/readers/{name}/taste")).StatusCode);
        (await client.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = true })).EnsureSuccessStatusCode();
        var shared = await anonymous.GetFromJsonAsync<JsonElement>($"/api/readers/{name}/taste");
        Assert.Equal("Distopya kâşifi", shared.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/readers/me/taste")).StatusCode);
    }

    private async Task<Guid> Novel()
    {
        var book = await CatalogTestData.CreateBookAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Editions.Add(new Edition(book, "Test Roman", "tur", EditionFormat.Paperback, coverImageUrl: "https://example.com/cover.jpg"));
        var dystopia = await db.Categories.SingleAsync(c => c.Slug == "distopya");
        db.BookCategories.Add(new BookCategory(book, dystopia.Id));
        await db.SaveChangesAsync();
        return book;
    }

    [Fact]
    public void A_strong_second_genre_flavours_the_identity()
    {
        var psychological = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var historical = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToArray();
        var genres = psychological.ToDictionary(id => id, _ => new[] { Psychological });
        foreach (var id in historical) genres[id] = [Historical];

        var profile = ReaderTaste.Build([.. psychological, .. historical], [], genres);

        Assert.Equal("Geçmişe tutkun iç dünyalar kâşifi", profile.Title);
        Assert.Null(profile.SignatureBookId);
    }

    [Fact]
    public void Signature_book_is_the_best_loved_rating_of_at_least_eight()
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var genres = ids.ToDictionary(id => id, _ => new[] { Historical });
        var best = Build(ids, [Rating(ids[0], 8, 8, 8, 8, 8, 8, 8), Rating(ids[1], 10, 10, 10, 10, 10, 10, 10), Rating(ids[2], 6, 6, 6, 6, 6, 6, 6)], genres);
        var lukewarm = Build(ids, ids.Select(id => Rating(id, 7, 7, 7, 7, 7, 7, 7)).ToArray(), genres);

        Assert.Equal(ids[1], best.SignatureBookId);
        Assert.Null(lukewarm.SignatureBookId);

        static ReaderTasteProfile Build(Guid[] _, PublicReaderRating[] rated, Dictionary<Guid, DiscoveryCategory[]> genres)
            => ReaderTaste.Build([], rated, genres);
    }
}
