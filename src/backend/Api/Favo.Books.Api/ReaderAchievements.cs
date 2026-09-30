using Favo.Books.Modules.Reading.Application;
using Favo.Books.Modules.Community.Application;
using Favo.Books.Modules.Profiles.Application;
namespace Favo.Books.Api;

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
