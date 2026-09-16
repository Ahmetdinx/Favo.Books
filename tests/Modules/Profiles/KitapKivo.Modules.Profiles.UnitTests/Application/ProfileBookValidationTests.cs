using KitapKivo.Modules.Profiles.Application.BedsideBooks.Set;
using KitapKivo.Modules.Profiles.Application.InfluentialBooks.Set;
using KitapKivo.Modules.Profiles.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Profiles.UnitTests.Application;

public sealed class ProfileBookValidationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingBook_IsRejectedBeforeAccessingProfilePersistence(bool bedside)
    {
        // No database provider: neither path may read or change profile selections.
        using var dbContext = new ProfilesDbContext(
            new DbContextOptionsBuilder<ProfilesDbContext>().Options);
        Guid bookId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var catalog = new MissingBookCatalog(bookId, cancellation.Token);

        if (bedside)
        {
            var handler = new SetBedsideBookHandler(dbContext, catalog);
            Assert.Null(await handler.HandleAsync(
                new SetBedsideBookCommand(Guid.NewGuid(), bookId, "Note"), cancellation.Token));
        }
        else
        {
            var handler = new SetInfluentialBookHandler(dbContext, catalog);
            Assert.Null(await handler.HandleAsync(
                new SetInfluentialBookCommand(Guid.NewGuid(), bookId, 1, "Note"), cancellation.Token));
        }

        Assert.True(catalog.WasCalled);
    }

    private sealed class MissingBookCatalog(Guid expectedId, CancellationToken expectedToken)
        : IBookCatalog
    {
        public bool WasCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid bookId, CancellationToken cancellationToken = default)
        {
            Assert.Equal(expectedId, bookId);
            Assert.Equal(expectedToken, cancellationToken);
            WasCalled = true;
            return Task.FromResult(false);
        }
    }
}
