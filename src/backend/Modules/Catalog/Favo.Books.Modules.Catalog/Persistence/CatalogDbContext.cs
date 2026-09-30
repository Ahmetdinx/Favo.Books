using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Categories;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Imports;
using Favo.Books.Modules.Catalog.Domain.Publishers;
using Favo.Books.Modules.Catalog.Domain.Sources;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    internal DbSet<Book> Books => Set<Book>();
    internal DbSet<NovelReview> NovelReviews => Set<NovelReview>();

    internal DbSet<Author> Authors => Set<Author>();

    internal DbSet<BookAuthor> BookAuthors =>
        Set<BookAuthor>();

    internal DbSet<Edition> Editions => Set<Edition>();

    internal DbSet<Publisher> Publishers =>
        Set<Publisher>();

    internal DbSet<ExternalReference>
        ExternalReferences =>
            Set<ExternalReference>();

    internal DbSet<Category> Categories =>
        Set<Category>();

    internal DbSet<BookCategory> BookCategories =>
        Set<BookCategory>();

    internal DbSet<CatalogImportJob> ImportJobs =>
        Set<CatalogImportJob>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CatalogDbContext).Assembly);
    }
}
