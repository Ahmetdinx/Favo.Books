using KitapKivo.Modules.Reading.Domain.LibraryEntries;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading.Persistence;

public sealed class ReadingDbContext : DbContext
{
    public ReadingDbContext(
        DbContextOptions<ReadingDbContext> options)
        : base(options)
    {
    }

    internal DbSet<LibraryEntry> LibraryEntries =>
        Set<LibraryEntry>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reading");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReadingDbContext).Assembly);
    }
}