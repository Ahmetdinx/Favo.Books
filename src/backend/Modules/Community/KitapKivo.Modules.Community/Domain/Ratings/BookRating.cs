namespace KitapKivo.Modules.Community.Domain.Ratings;

internal sealed class BookRating
{
    private BookRating() { }

    private BookRating(Guid userId, Guid bookId, RatingScores scores, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BookId = bookId;
        SetScores(scores);
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    // Existing 1-5 votes are preserved without inventing per-criterion scores.
    public int? LegacyScore { get; private set; }
    public int? Theme { get; private set; }
    public int? Language { get; private set; }
    public int? Characters { get; private set; }
    public int? Structure { get; private set; }
    public int? Pace { get; private set; }
    public int? Impact { get; private set; }
    public int? Experience { get; private set; }
    public decimal? OverallScore { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static BookRating Create(Guid userId, Guid bookId, RatingScores scores, DateTimeOffset now)
    {
        EnsureId(userId, nameof(userId));
        EnsureId(bookId, nameof(bookId));
        EnsureUtc(now, nameof(now));
        return new(userId, bookId, scores, now);
    }

    public void ChangeScores(RatingScores scores, DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        if (now < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(now));
        }

        SetScores(scores);
        UpdatedAtUtc = now;
    }

    private void SetScores(RatingScores scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        Theme = scores.Theme;
        Language = scores.Language;
        Characters = scores.Characters;
        Structure = scores.Structure;
        Pace = scores.Pace;
        Impact = scores.Impact;
        Experience = scores.Experience;
        OverallScore = scores.OverallScore;
        LegacyScore = null;
    }

    private static void EnsureId(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Kimlik boş olamaz.", name);
    }

    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Tarih UTC olmalıdır.", name);
    }
}
