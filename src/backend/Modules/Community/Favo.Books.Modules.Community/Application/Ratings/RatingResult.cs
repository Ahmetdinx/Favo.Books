using Favo.Books.Modules.Community.Domain.Ratings;

namespace Favo.Books.Modules.Community.Application.Ratings;

internal sealed record RatingResult(
    Guid BookId,
    decimal? AverageScore,
    int RatingCount,
    CriterionAverages? CriteriaAverages,
    UserRatingResult? CurrentUserRating,
    int LegacyRatingCount,
    int? CurrentUserLegacyScore)
{
    public int MinimumScore => RatingPolicy.MinimumScore;
    public int MaximumScore => RatingPolicy.MaximumScore;
    public IReadOnlyList<RatingCriterion> Criteria => RatingPolicy.Criteria;
}

internal sealed record CriterionAverages(
    decimal Theme, decimal Language, decimal Characters, decimal Structure,
    decimal Pace, decimal Impact, decimal Experience);

internal sealed record UserRatingResult(CriterionScores Scores, decimal OverallScore);

internal sealed record CriterionScores(
    int Theme, int Language, int Characters, int Structure,
    int Pace, int Impact, int Experience);
