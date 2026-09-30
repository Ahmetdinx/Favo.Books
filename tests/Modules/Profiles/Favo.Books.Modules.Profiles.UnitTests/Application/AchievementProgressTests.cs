using Favo.Books.Modules.Profiles.Application;

namespace Favo.Books.Modules.Profiles.UnitTests.Application;

public sealed class AchievementProgressTests
{
    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 1, 10)]
    [InlineData(9, 1, 10)]
    [InlineData(10, 10, 100)]
    [InlineData(99, 10, 100)]
    [InlineData(100, 100, 1000)]
    [InlineData(999, 100, 1000)]
    [InlineData(1000, 1000, 0)]
    [InlineData(1001, 1000, 0)]
    public void ThresholdsAndRemainingAreCorrectForBothTracks(int count, int current, int next)
    {
        foreach (var track in new[] { AchievementProgress.Reading(count), AchievementProgress.Evaluation(count) })
        {
            Assert.Equal(current, track.Current?.Threshold ?? 0);
            Assert.Equal(next, track.Next?.Threshold ?? 0);
            Assert.Equal(next == 0 ? 0 : next - count, track.Remaining);
            Assert.InRange(track.ProgressPercent, 0, 100);
            if (next == 0) Assert.Equal(100, track.ProgressPercent);
            else if (count == current) Assert.Equal(0, track.ProgressPercent);
        }
    }

    [Fact]
    public void ReadingAndEvaluationHaveDifferentIdentityAndTitles()
    {
        Assert.NotEqual(AchievementProgress.Reading(100).Current!.Title, AchievementProgress.Evaluation(100).Current!.Title);
        Assert.Throws<ArgumentOutOfRangeException>(() => AchievementProgress.Reading(-1));
    }
}
