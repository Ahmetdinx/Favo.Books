using KitapKivo.Modules.Catalog.Domain.Authors;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Domain.Publishers;
using KitapKivo.Modules.Catalog.Domain.Sources;
using KitapKivo.Modules.Catalog.Domain.Categories;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    internal DbSet<Book> Books => Set<Book>();

    internal DbSet<Author> Authors => Set<Author>();

    internal DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    internal DbSet<Edition> Editions => Set<Edition>();

    internal DbSet<Publisher> Publishers => Set<Publisher>();

    internal DbSet<ExternalReference> ExternalReferences =>
        Set<ExternalReference>();
    internal DbSet<Category> Categories => Set<Category>();

    internal DbSet<BookCategory> BookCategories =>
        Set<BookCategory>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CatalogDbContext).Assembly);
    }
}