namespace Favo.Books.Modules.Catalog.Domain.Books;

public enum NovelDecision { Unknown, Novel, NotNovel, SelectedClassic }

internal sealed class NovelReview
{
    private NovelReview() { }

    public NovelReview(Guid bookId, NovelDecision decision, string batchId, string sourceUrl,
        string evidenceNote, string reviewer, string policyVersion)
    {
        if (bookId == Guid.Empty) throw new ArgumentException("Book ID is required.", nameof(bookId));
        if (!Enum.IsDefined(decision)) throw new ArgumentOutOfRangeException(nameof(decision));
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new ArgumentException("An HTTPS evidence URL is required.", nameof(sourceUrl));
        BookId = bookId;
        Decision = decision;
        BatchId = Required(batchId, 100);
        SourceUrl = Required(sourceUrl, 2000);
        EvidenceNote = Required(evidenceNote, 2000);
        Reviewer = Required(reviewer, 100);
        PolicyVersion = Required(policyVersion, 100);
        RecordedAtUtc = DateTimeOffset.UtcNow;
    }

    public long Id { get; private set; }
    public Guid BookId { get; private set; }
    public NovelDecision Decision { get; private set; }
    public string BatchId { get; private set; } = "";
    public string SourceUrl { get; private set; } = "";
    public string EvidenceNote { get; private set; } = "";
    public string Reviewer { get; private set; } = "";
    public string PolicyVersion { get; private set; } = "";
    public DateTimeOffset RecordedAtUtc { get; private set; }

    private static string Required(string value, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Trim().Length > maxLength) throw new ArgumentException("Review field exceeds its limit.");
        return value.Trim();
    }
}
