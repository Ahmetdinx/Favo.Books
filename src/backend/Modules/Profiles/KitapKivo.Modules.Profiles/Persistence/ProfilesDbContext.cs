using KitapKivo.Modules.Profiles
    .Domain.BedsideBooks;
using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Profiles.Persistence;

public sealed class ProfilesDbContext :
    DbContext
{
    public ProfilesDbContext(
        DbContextOptions<ProfilesDbContext>
            options)
        : base(options)
    {
    }

    internal DbSet<BedsideBook>
        BedsideBooks =>
            Set<BedsideBook>();

    internal DbSet<InfluentialBook>
        InfluentialBooks =>
            Set<InfluentialBook>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(
            "profiles");

        modelBuilder
            .ApplyConfigurationsFromAssembly(
                typeof(ProfilesDbContext)
                    .Assembly);
    }
}