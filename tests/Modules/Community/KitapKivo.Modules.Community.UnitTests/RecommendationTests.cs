using KitapKivo.Modules.Community.Application.Ratings;
using KitapKivo.Modules.Community.Domain.Ratings;
using KitapKivo.SharedKernel.Catalog;

namespace KitapKivo.Modules.Community.UnitTests;

public sealed class RecommendationTests
{
    [Fact]
    public void DismissalOnlyExcludesOwnChosenBookAndUndoRestoresIt()
    {
        var book = Book(); var other = Book();
        var feedback = new RecommendationFeedback { UserId = _user, BookId = book.BookId, Choice = "dismissed" };
        var result = BookRecommendations.Rank([book, other], [], [], _user, new(), [feedback]);
        Assert.Equal(other.BookId, Assert.Single(result.Items).BookId);
        Assert.Single(result.Feedback);
        Assert.Equal(2, BookRecommendations.Rank([book, other], [], [], Guid.NewGuid(), new(), [feedback]).Items.Length);
        Assert.Equal(2, BookRecommendations.Rank([book, other], [], [], _user, new(), []).Items.Length);
    }

    [Fact]
    public void InterestBoostsRelatedBooksWithoutPretendingTheyWereReadOrRated()
    {
        var interested = Book("siir"); var related = Book("siir"); var unrelated = Book("tarih");
        var feedback = new RecommendationFeedback { UserId = _user, BookId = interested.BookId, Choice = "interested" };
        var result = BookRecommendations.Rank([interested, unrelated, related], [], [], _user, new(), [feedback]);
        Assert.Equal(related.BookId, result.Items[0].BookId);
        Assert.Contains(result.Items[0].Reasons, r => r.Contains("İlgini çeken"));
        Assert.All(result.Items, b => Assert.Equal(0, b.RatingCount));
        Assert.Contains(result.Items, b => b.BookId == interested.BookId);
        Assert.True(result.HasPersonalSignals);
    }

    private readonly Guid _user = Guid.NewGuid();
    private static DiscoveryBook Book(string genre = "roman", Guid? author = null) => new(
        Guid.NewGuid(), "Bir kitap", "https://example.test/cover.jpg", [author ?? Guid.NewGuid()], [new(genre, genre)]);
    private static BookRating Vote(Guid book, Guid user, int pace, int language) => BookRating.Create(
        user, book, new RatingScores(8, language, 8, 8, pace, 8, 8), DateTimeOffset.UtcNow);

    [Fact]
    public void ReadReadingAndAlreadyRatedBooksAreExcluded()
    {
        var read = Book(); var reading = Book(); var rated = Book(); var fresh = Book();
        var result = BookRecommendations.Rank([read, reading, rated, fresh], [Vote(rated.BookId, _user, 8, 8)],
            [read.BookId, reading.BookId], _user, new());
        Assert.Equal(fresh.BookId, Assert.Single(result.Items).BookId);
    }

    [Fact]
    public void ExplicitCriterionChangesOrderAndExplainsEvidence()
    {
        var fast = Book(); var lyrical = Book();
        var votes = Enumerable.Range(0, 3).SelectMany(_ => new[] {
            Vote(fast.BookId, Guid.NewGuid(), 10, 3), Vote(lyrical.BookId, Guid.NewGuid(), 3, 10) }).ToArray();
        var pace = BookRecommendations.Rank([fast, lyrical], votes, [], _user, new() { Criteria = ["pace"] });
        var language = BookRecommendations.Rank([fast, lyrical], votes, [], _user, new() { Criteria = ["language"] });
        Assert.Equal(fast.BookId, pace.Items[0].BookId);
        Assert.Equal(lyrical.BookId, language.Items[0].BookId);
        Assert.Contains(pace.Items[0].Reasons, r => r.Contains("3 değerlendirme") && r.Contains("Tempo"));
    }

    [Fact]
    public void SparseVotesDoNotClaimCriterionMatchAndColdStartIsHonest()
    {
        var book = Book();
        var result = BookRecommendations.Rank([book], [Vote(book.BookId, Guid.NewGuid(), 10, 10)], [], _user,
            new() { Criteria = ["pace"] });
        Assert.Contains("Keşif önerisi", Assert.Single(Assert.Single(result.Items).Reasons));
        Assert.False(BookRecommendations.Rank([book], [], [], _user, new()).HasPersonalSignals);
    }

    [Fact]
    public void LikedBooksProvideGenreAndAuthorReasonsWithoutInventingPriorities()
    {
        Guid author = Guid.NewGuid();
        var liked = Book(author: author); var related = Book(author: author); var unrelated = Book("tarih");
        var result = BookRecommendations.Rank([liked, related, unrelated], [Vote(liked.BookId, _user, 9, 9)], [], _user, new());
        Assert.Equal(related.BookId, result.Items[0].BookId);
        Assert.Contains(result.Items[0].Reasons, r => r.Contains("aynı yazarı"));
        Assert.DoesNotContain(result.Items[0].Reasons, r => r.Contains("Önem verdiğin"));
    }

    [Fact]
    public void ExplicitGenreWinsAndResultsAreBoundedAndStable()
    {
        var chosen = Book("siir");
        var books = Enumerable.Range(0, 10).Select(_ => Book()).Append(chosen).ToArray();
        var preference = new ReadingPreference { Categories = ["siir"] };
        var result = BookRecommendations.Rank(books, [], [], _user, preference);
        Assert.Equal(chosen.BookId, result.Items[0].BookId);
        Assert.Equal(6, result.Items.Length);
        Assert.Equal(result.Items.Select(b => b.BookId), BookRecommendations.Rank(books.Reverse().ToArray(), [], [], _user, preference).Items.Select(b => b.BookId));
    }
}
