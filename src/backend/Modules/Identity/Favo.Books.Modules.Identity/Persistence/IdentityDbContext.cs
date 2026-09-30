using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens =>
    Set<RefreshToken>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IdentityDbContext).Assembly);
    }
}