using Favo.Books.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Favo.Books.Api.IntegrationTests.Community;

public sealed class RatingMigrationTests
{
    [Fact]
    public async Task Upgrade_PreservesLegacyVotes_AndEnforcesCompleteWeightedScores()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        await using var db = new CommunityDbContext(new DbContextOptionsBuilder<CommunityDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260916065625_InitialCommunity");
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO community."BookRatings" ("Id", "UserId", "BookId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES ({id}, {userId}, {bookId}, 5, {now}, {now})
            """);
        await migrator.MigrateAsync();
        var vote = await db.BookRatings.SingleAsync();
        Assert.Equal(id, vote.Id);
        Assert.Equal(5, vote.LegacyScore);
        Assert.Null(vote.Theme);
        Assert.Null(vote.OverallScore);
        // PostgreSQL must reject both incomplete input and an invented overall score.
        var incomplete = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE community.\"BookRatings\" SET \"LegacyScore\" = NULL, \"Theme\" = 8"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, incomplete.SqlState);
        var tampered = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("""
            UPDATE community."BookRatings" SET "LegacyScore" = NULL,
            "Theme" = 8, "Language" = 8, "Characters" = 8, "Structure" = 8,
            "Pace" = 8, "Impact" = 8, "Experience" = 8, "OverallScore" = 10
            """));
        Assert.Equal(PostgresErrorCodes.CheckViolation, tampered.SqlState);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE community."BookRatings" SET "LegacyScore" = NULL,
            "Theme" = 8, "Language" = 8, "Characters" = 8, "Structure" = 8,
            "Pace" = 8, "Impact" = 8, "Experience" = 8, "OverallScore" = 8
            """
        );
        await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20260916065625_InitialCommunity"));
        db.ChangeTracker.Clear();
        Assert.Equal(8m, (await db.BookRatings.SingleAsync()).OverallScore);
    }
}
