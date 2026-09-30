using Favo.Books.Modules.Catalog.Domain.Books;

namespace Favo.Books.Modules.Catalog.UnitTests.Domain.Books;

public sealed class NovelEvidenceClassifierTests
{
    [Theory]
    [InlineData("fiction")]
    [InlineData("historical fiction")]
    [InlineData("history")]
    [InlineData("finance")]
    [InlineData("classics")]
    [InlineData("novels, history and criticism")]
    [InlineData("Roman Empire")]
    [InlineData("fictional biography")]
    public void BroadSubjectsAreNotLiteraryFormEvidence(string subject)
        => Assert.Equal(NovelEvidenceStatus.Unknown, NovelEvidenceClassifier.Classify([subject]).Status);

    [Theory]
    [InlineData("Novels")]
    [InlineData("  historical   novels  ")]
    [InlineData("ROMANLAR")]
    [InlineData("TARİHÎ ROMAN")]
    public void ExplicitNovelFormIsCandidateEvenWithHistorySubject(string subject)
        => Assert.Equal(NovelEvidenceStatus.NovelCandidate,
            NovelEvidenceClassifier.Classify([subject, "history"]).Status);

    [Theory]
    [InlineData("Short stories")]
    [InlineData("Poetry")]
    [InlineData("Non-fiction")]
    [InlineData("Self-help")]
    [InlineData("graphic novels")]
    public void OtherFormsAreNotNovels(string subject)
        => Assert.Equal(NovelEvidenceStatus.NonNovelCandidate,
            NovelEvidenceClassifier.Classify([subject, "fiction"]).Status);

    [Fact]
    public void ConflictingEvidenceIsPreservedForReview()
    {
        var result = NovelEvidenceClassifier.Classify(["novels", "short stories", "novels"]);
        Assert.Equal(NovelEvidenceStatus.Conflict, result.Status);
        Assert.Equal(["novels"], result.NovelSignals);
        Assert.Equal(["short stories"], result.NonNovelSignals);
        Assert.Equal(NovelEvidenceClassifier.Version, result.RuleVersion);
    }

    [Fact]
    public void MissingEvidenceStaysUnknown()
        => Assert.Equal(NovelEvidenceStatus.Unknown, NovelEvidenceClassifier.Classify([]).Status);
}
