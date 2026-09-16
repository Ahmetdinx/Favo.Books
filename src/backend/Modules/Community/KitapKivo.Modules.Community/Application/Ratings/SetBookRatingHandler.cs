using KitapKivo.Modules.Community.Domain.Ratings;
using KitapKivo.Modules.Community.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Community.Application.Ratings;

internal sealed class SetBookRatingHandler(
    CommunityDbContext dbContext,
    IBookCatalog bookCatalog,
    GetBookRatingsHandler getRatings)
{
    public async Task<RatingResult?> HandleAsync(
        Guid userId, Guid bookId, RatingScores scores,
        CancellationToken cancellationToken = default)
    {
        if (!await bookCatalog.ExistsAsync(bookId, cancellationToken)) return null;
        BookRating rating = BookRating.Create(userId, bookId, scores, DateTimeOffset.UtcNow);

        // Atomic upsert handles concurrent submissions and upgrades legacy votes
        // while retaining the row identity and creation timestamp.
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO community."BookRatings"
                ("Id", "UserId", "BookId", "Theme", "Language", "Characters",
                 "Structure", "Pace", "Impact", "Experience", "OverallScore",
                 "LegacyScore", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES
                ({rating.Id}, {rating.UserId}, {rating.BookId}, {rating.Theme},
                 {rating.Language}, {rating.Characters}, {rating.Structure},
                 {rating.Pace}, {rating.Impact}, {rating.Experience}, {rating.OverallScore},
                 NULL, {rating.CreatedAtUtc}, {rating.UpdatedAtUtc})
            ON CONFLICT ("UserId", "BookId") DO UPDATE SET
                "Theme" = EXCLUDED."Theme",
                "Language" = EXCLUDED."Language",
                "Characters" = EXCLUDED."Characters",
                "Structure" = EXCLUDED."Structure",
                "Pace" = EXCLUDED."Pace",
                "Impact" = EXCLUDED."Impact",
                "Experience" = EXCLUDED."Experience",
                "OverallScore" = EXCLUDED."OverallScore",
                "LegacyScore" = NULL,
                "UpdatedAtUtc" = GREATEST("BookRatings"."UpdatedAtUtc", EXCLUDED."UpdatedAtUtc")
            """, cancellationToken);

        return await getRatings.HandleAsync(bookId, userId, cancellationToken);
    }
}
