namespace KitapKivo.Modules.Community.Domain.Ratings;

internal sealed class ReadingPreference
{
    public Guid UserId { get; set; }
    public string[] Categories { get; set; } = [];
    public string[] Criteria { get; set; } = [];
}
