using KitapKivo.Modules.Community.Domain.Ratings;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Community.Persistence;

public sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options)
    : DbContext(options)
{
    internal DbSet<BookRating> BookRatings => Set<BookRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("community");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
    }
}
