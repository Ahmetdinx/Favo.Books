using Favo.Books.Modules.Community.Application.Ratings;
using Favo.Books.Modules.Community.Domain.Ratings;
using Favo.Books.SharedKernel.Catalog;

namespace Favo.Books.Modules.Community.UnitTests;

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
        Assert.DoesNotContain(result.Items, b => b.BookId == interested.BookId);
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
        Assert.Single(result.Items);
        Assert.Equal(result.Items.Select(b => b.BookId), BookRecommendations.Rank(books.Reverse().ToArray(), [], [], _user, preference).Items.Select(b => b.BookId));
    }

    [Fact]
    public void PerfectAndCrossGenreShelvesRequireEveryPriorityAboveNineAndThreeVotes()
    {
        var perfect = Book(); var discovery = Book("fantastik"); var boundary = Book(); var sparse = Book();
        BookRating Strong(DiscoveryBook book, int pace = 10) => BookRating.Create(Guid.NewGuid(), book.BookId,
            new RatingScores(10, 10, 10, 10, pace, 10, 10), DateTimeOffset.UtcNow);
        var votes = Enumerable.Range(0, 3).SelectMany(_ => new[] { Strong(perfect), Strong(discovery), Strong(boundary, 9) })
            .Append(Strong(sparse)).ToArray();
        var result = BookRecommendations.Rank([boundary, discovery, sparse, perfect], votes, [], _user,
            new() { Categories = ["roman"], Criteria = ["pace", "language", "characters"] });
        Assert.Equal(perfect.BookId, result.Items[0].BookId);
        Assert.Equal("perfect", result.Items[0].Shelf);
        Assert.Equal("discovery", result.Items.Single(b => b.BookId == discovery.BookId).Shelf);
        Assert.Equal("personal", result.Items.Single(b => b.BookId == boundary.BookId).Shelf);
        Assert.Empty(result.Items.Single(b => b.BookId == sparse.BookId).PriorityScores);
        Assert.Equal(result.Items.Length, result.Items.Select(b => b.BookId).Distinct().Count());
        var fewer = BookRecommendations.Rank([perfect], votes, [], _user,
            new() { Categories = ["roman"], Criteria = ["pace", "language"] });
        Assert.Equal("personal", Assert.Single(fewer.Items).Shelf);
    }

    [Fact]
    public void PreviewUsesRealRankingAndReplacesChosenBooks()
    {
        var initial = RecommendationPreview.Create([]);
        Assert.Contains(initial.Items, b => b.Shelf == "perfect");
        Assert.Contains(initial.Items, b => b.Shelf == "discovery");
        Assert.Equal("personal", initial.Items.Single(b => b.Title.StartsWith("Sınır testi")).Shelf);
        Assert.Equal("personal", initial.Items.Single(b => b.Title.StartsWith("Az veri")).Shelf);
        var next = RecommendationPreview.Create([initial.Items[0].BookId]);
        Assert.DoesNotContain(next.Items, b => b.BookId == initial.Items[0].BookId);
        Assert.Equal(6, next.Items.Count(b => b.Shelf == "perfect"));
        Assert.Contains(next.Items, b => !initial.Items.Any(old => old.BookId == b.BookId));
    }

    [Fact]
    public void LibraryShelfUsesOnlySavedCandidatesAndRetainsOtherGenres()
    {
        var chosen = Book(); var different = Book("fantastik"); var outside = Book(); var rated = Book();
        var preference = new ReadingPreference { Categories = ["roman"] };
        var result = BookRecommendations.Rank([different, outside, chosen, rated], [Vote(rated.BookId, _user, 8, 8)], [], _user,
            preference, [new RecommendationFeedback { UserId = _user, BookId = chosen.BookId, Choice = "interested" }],
            [chosen.BookId, different.BookId, rated.BookId]);
        Assert.Equal(chosen.BookId, result.Items[0].BookId);
        Assert.Contains(result.Items, b => b.BookId == different.BookId);
        Assert.DoesNotContain(result.Items, b => b.BookId == outside.BookId || b.BookId == rated.BookId);
        Assert.Empty(BookRecommendations.Rank([chosen], [], [], _user, preference, libraryCandidates: []).Items);
    }

    [Fact]
    public void LibraryShelfIsBoundedToThree()
    {
        var books = Enumerable.Range(0, 10).Select(_ => Book()).ToArray();
        var result = BookRecommendations.Rank(books, [], [], _user, new(), libraryCandidates: books.Select(b => b.BookId).ToHashSet());
        Assert.Equal(3, result.Items.Length);
    }
}
