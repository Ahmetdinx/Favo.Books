using Favo.Books.Modules.Community.Domain.Ratings;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Community.Persistence;

public sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options)
    : DbContext(options)
{
    internal DbSet<BookRating> BookRatings => Set<BookRating>();
    internal DbSet<ReadingPreference> ReadingPreferences => Set<ReadingPreference>();
    internal DbSet<RecommendationFeedback> RecommendationFeedback => Set<RecommendationFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("community");
        modelBuilder.Entity<ReadingPreference>().HasKey(p => p.UserId);
        modelBuilder.Entity<RecommendationFeedback>().HasKey(p => new { p.UserId, p.BookId });
        modelBuilder.Entity<RecommendationFeedback>().Property(p => p.Choice).HasMaxLength(16);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
    }
}
