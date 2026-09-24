using KitapKivo.Modules.Reading.Application;
using KitapKivo.Modules.Community.Application;
using KitapKivo.Modules.Profiles.Application;
namespace KitapKivo.Api;

public sealed class ReaderAchievements(PublicLibrary library, PublicReaderRatings ratings)
{
    public async Task<AchievementTrack[]> Get(Guid userId, CancellationToken ct)
    {
        // Sequential: both counters share Catalog's scoped DbContext.
        var read = await library.CountCompletedNovels(userId, ct);
        var evaluated = await ratings.CountEvaluatedNovels(userId, ct);
        return [AchievementProgress.Reading(read), AchievementProgress.Evaluation(evaluated)];
    }
}
