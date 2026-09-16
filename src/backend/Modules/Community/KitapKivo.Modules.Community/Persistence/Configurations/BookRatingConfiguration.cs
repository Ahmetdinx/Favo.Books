using KitapKivo.Modules.Community.Domain.Ratings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Community.Persistence.Configurations;

internal sealed class BookRatingConfiguration : IEntityTypeConfiguration<BookRating>
{
    public void Configure(EntityTypeBuilder<BookRating> builder)
    {
        builder.ToTable("BookRatings", table =>
        {
            // Explicit NULL checks are required: SQL CHECK permits UNKNOWN.
            table.HasCheckConstraint("CK_BookRatings_CompleteScores", """
                (
                    "LegacyScore" IS NOT NULL AND "LegacyScore" BETWEEN 1 AND 5
                    AND "Theme" IS NULL AND "Language" IS NULL AND "Characters" IS NULL
                    AND "Structure" IS NULL AND "Pace" IS NULL AND "Impact" IS NULL
                    AND "Experience" IS NULL AND "OverallScore" IS NULL
                )
                OR
                (
                    "LegacyScore" IS NULL
                    AND "Theme" IS NOT NULL AND "Theme" BETWEEN 1 AND 10
                    AND "Language" IS NOT NULL AND "Language" BETWEEN 1 AND 10
                    AND "Characters" IS NOT NULL AND "Characters" BETWEEN 1 AND 10
                    AND "Structure" IS NOT NULL AND "Structure" BETWEEN 1 AND 10
                    AND "Pace" IS NOT NULL AND "Pace" BETWEEN 1 AND 10
                    AND "Impact" IS NOT NULL AND "Impact" BETWEEN 1 AND 10
                    AND "Experience" IS NOT NULL AND "Experience" BETWEEN 1 AND 10
                    AND "OverallScore" IS NOT NULL
                    AND "OverallScore" =
                        (15 * ("Theme" + "Language" + "Characters" + "Structure" + "Impact" + "Experience")
                         + 10 * "Pace") / 100.0
                )
                """);
        });
        builder.HasKey(rating => rating.Id);
        builder.Property(rating => rating.UserId).IsRequired();
        builder.Property(rating => rating.BookId).IsRequired();
        builder.Property(rating => rating.OverallScore).HasPrecision(4, 2);
        builder.Property(rating => rating.CreatedAtUtc).IsRequired();
        builder.Property(rating => rating.UpdatedAtUtc).IsRequired();
        builder.HasIndex(rating => new { rating.UserId, rating.BookId }).IsUnique();
        builder.HasIndex(rating => rating.BookId);
    }
}
