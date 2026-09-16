using KitapKivo.Modules.Reading.Application.LibraryEntries.Add;
using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading.UnitTests.Application;

public sealed class AddBookToLibraryHandlerTests
{
    [Fact]
    public async Task MissingBook_IsRejectedBeforeAccessingReadingPersistence()
    {
        // No database provider: touching Reading persistence would fail this test.
        using var dbContext = new ReadingDbContext(
            new DbContextOptionsBuilder<ReadingDbContext>().Options);
        Guid bookId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var catalog = new MissingBookCatalog(bookId, cancellation.Token);
        var handler = new AddBookToLibraryHandler(dbContext, catalog);

        var result = await handler.HandleAsync(
            new AddBookToLibraryCommand(Guid.NewGuid(), bookId, LibraryEntryStatus.WantToRead),
            cancellation.Token);

        Assert.Equal(AddBookToLibraryError.BookNotFound, result.Error);
        Assert.Null(result.LibraryEntryId);
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
