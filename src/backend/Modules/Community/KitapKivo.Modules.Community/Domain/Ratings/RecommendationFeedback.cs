namespace KitapKivo.Modules.Community.Domain.Ratings;

internal sealed class RecommendationFeedback
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public string Choice { get; set; } = "";
}
