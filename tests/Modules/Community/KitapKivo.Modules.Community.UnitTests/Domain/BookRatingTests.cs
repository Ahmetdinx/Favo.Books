using KitapKivo.Modules.Community.Domain.Ratings;

namespace KitapKivo.Modules.Community.UnitTests.Domain;

public sealed class BookRatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void AllEqualScores_KeepSameOverall(int score)
    {
        RatingScores scores = Uniform(score);
        Assert.Equal((decimal)score, scores.OverallScore);
    }

    [Fact]
    public void MixedScores_UseRequestedWeightsExactly()
    {
        var scores = new RatingScores(8, 9, 7, 8, 6, 9, 8);
        Assert.Equal(7.95m, scores.OverallScore);
        Assert.Equal(100, RatingPolicy.Criteria.Sum(criterion => criterion.Weight));
        Assert.Equal(7, RatingPolicy.Criteria.Select(criterion => criterion.Key).Distinct().Count());
    }

    [Theory]
    [InlineData(0, 1.35)]
    [InlineData(1, 1.35)]
    [InlineData(2, 1.35)]
    [InlineData(3, 1.35)]
    [InlineData(4, 0.90)]
    [InlineData(5, 1.35)]
    [InlineData(6, 1.35)]
    public void EachCriterion_HasItsOwnWeight(int index, double increase)
    {
        int[] values = [1, 1, 1, 1, 1, 1, 1];
        values[index] = 10;
        var scores = From(values);
        Assert.Equal(1m + (decimal)increase, scores.OverallScore);
    }

    public static IEnumerable<object[]> InvalidScores()
    {
        for (int index = 0; index < 7; index++)
        {
            yield return new object[] { index, 0 };
            yield return new object[] { index, 11 };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidScores))]
    public void EveryCriterion_RejectsOutOfRangeValues(int index, int value)
    {
        int[] scores = [5, 5, 5, 5, 5, 5, 5];
        scores[index] = value;
        Assert.Throws<ArgumentOutOfRangeException>(() => From(scores));
    }

    [Fact]
    public void ChangeScores_PreservesIdentityAndRecalculatesOverall()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        BookRating rating = BookRating.Create(Guid.NewGuid(), Guid.NewGuid(), Uniform(3), now);
        Guid id = rating.Id;
        rating.ChangeScores(new(8, 9, 7, 8, 6, 9, 8), now.AddMinutes(1));

        Assert.Equal(id, rating.Id);
        Assert.Equal(now, rating.CreatedAtUtc);
        Assert.Equal(now.AddMinutes(1), rating.UpdatedAtUtc);
        Assert.Equal(7.95m, rating.OverallScore);
        Assert.Equal(6, rating.Pace);
        Assert.Equal(8, rating.Experience);
        Assert.Null(rating.LegacyScore);
    }

    [Fact]
    public void EarlierUpdate_LeavesAllScoresUnchanged()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        BookRating rating = BookRating.Create(Guid.NewGuid(), Guid.NewGuid(), Uniform(3), now);
        rating.ChangeScores(Uniform(8), now.AddMinutes(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => rating.ChangeScores(Uniform(1), now));
        Assert.Equal(8m, rating.OverallScore);
    }

    [Fact]
    public void InvalidIdentityAndNonUtcDate_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => BookRating.Create(
            Guid.Empty, Guid.NewGuid(), Uniform(5), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => BookRating.Create(
            Guid.NewGuid(), Guid.Empty, Uniform(5), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => BookRating.Create(
            Guid.NewGuid(), Guid.NewGuid(), Uniform(5), DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3))));
    }

    private static RatingScores Uniform(int score) => new(score, score, score, score, score, score, score);
    private static RatingScores From(int[] scores) => new(
        scores[0], scores[1], scores[2], scores[3], scores[4], scores[5], scores[6]);
}
