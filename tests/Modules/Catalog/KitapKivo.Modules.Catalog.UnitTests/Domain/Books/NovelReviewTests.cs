using KitapKivo.Modules.Catalog.Domain.Books;

namespace KitapKivo.Modules.Catalog.UnitTests.Domain.Books;

public sealed class NovelReviewTests
{
    [Theory]
    [InlineData(NovelDecision.Unknown)]
    [InlineData(NovelDecision.Novel)]
    [InlineData(NovelDecision.NotNovel)]
    public void StoresExplicitDecisionAndProvenance(NovelDecision decision)
    {
        var review = new NovelReview(Guid.NewGuid(), decision, " batch ", "https://publisher.example/book",
            " evidence ", " reviewer ", "v1");
        Assert.Equal(decision, review.Decision);
        Assert.Equal("batch", review.BatchId);
        Assert.Equal("evidence", review.EvidenceNote);
        Assert.Equal(TimeSpan.Zero, review.RecordedAtUtc.Offset);
    }

    [Theory]
    [InlineData("http://publisher.example/book")]
    [InlineData("not a URL")]
    [InlineData("")]
    public void RejectsMissingOrInsecureEvidence(string source)
        => Assert.Throws<ArgumentException>(() => new NovelReview(Guid.NewGuid(), NovelDecision.Novel,
            "batch", source, "evidence", "reviewer", "v1"));

    [Fact]
    public void RejectsMissingReasonAndInvalidDecision()
    {
        Assert.Throws<ArgumentException>(() => new NovelReview(Guid.NewGuid(), NovelDecision.Novel,
            "batch", "https://publisher.example/book", " ", "reviewer", "v1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NovelReview(Guid.NewGuid(), (NovelDecision)999,
            "batch", "https://publisher.example/book", "evidence", "reviewer", "v1"));
    }
}
