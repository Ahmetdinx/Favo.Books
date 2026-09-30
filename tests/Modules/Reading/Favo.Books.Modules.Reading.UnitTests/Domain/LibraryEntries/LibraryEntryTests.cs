using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading.UnitTests.Domain.LibraryEntries;

public sealed class LibraryEntryTests
{
    private static readonly Guid UserId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid BookId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldCreateEntry_WhenValuesAreValid()
    {
        LibraryEntry entry = LibraryEntry.Create(
            UserId,
            BookId,
            LibraryEntryStatus.WantToRead,
            CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(UserId, entry.UserId);
        Assert.Equal(BookId, entry.BookId);
        Assert.Equal(
            LibraryEntryStatus.WantToRead,
            entry.Status);
        Assert.Equal(
            CreatedAtUtc,
            entry.CreatedAtUtc);
        Assert.Equal(
            CreatedAtUtc,
            entry.UpdatedAtUtc);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                LibraryEntry.Create(
                    Guid.Empty,
                    BookId,
                    LibraryEntryStatus.WantToRead,
                    CreatedAtUtc));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenBookIdIsEmpty()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                LibraryEntry.Create(
                    UserId,
                    Guid.Empty,
                    LibraryEntryStatus.WantToRead,
                    CreatedAtUtc));

        Assert.Equal("bookId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrowArgumentOutOfRangeException_WhenStatusIsInvalid()
    {
        var invalidStatus = (LibraryEntryStatus)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LibraryEntry.Create(
                UserId,
                BookId,
                invalidStatus,
                CreatedAtUtc));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenCreatedAtIsNotUtc()
    {
        var nonUtcDate = new DateTimeOffset(
            2026,
            8,
            7,
            15,
            0,
            0,
            TimeSpan.FromHours(3));

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                LibraryEntry.Create(
                    UserId,
                    BookId,
                    LibraryEntryStatus.WantToRead,
                    nonUtcDate));

        Assert.Equal("createdAtUtc", exception.ParamName);
    }

    [Fact]
    public void ChangeStatus_ShouldUpdateStatusAndTimestamp()
    {
        LibraryEntry entry = CreateEntry();
        DateTimeOffset updatedAtUtc =
            CreatedAtUtc.AddHours(1);

        entry.ChangeStatus(
            LibraryEntryStatus.Reading,
            updatedAtUtc);

        Assert.Equal(
            LibraryEntryStatus.Reading,
            entry.Status);
        Assert.Equal(
            updatedAtUtc,
            entry.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_ShouldNotUpdateTimestamp_WhenStatusIsSame()
    {
        LibraryEntry entry = CreateEntry();

        entry.ChangeStatus(
            LibraryEntryStatus.WantToRead,
            CreatedAtUtc.AddHours(1));

        Assert.Equal(
            CreatedAtUtc,
            entry.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeStatus_ShouldThrowArgumentOutOfRangeException_WhenStatusIsInvalid()
    {
        LibraryEntry entry = CreateEntry();
        var invalidStatus = (LibraryEntryStatus)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            entry.ChangeStatus(
                invalidStatus,
                CreatedAtUtc.AddHours(1)));
    }

    [Fact]
    public void ChangeStatus_ShouldThrowArgumentException_WhenUpdatedAtIsNotUtc()
    {
        LibraryEntry entry = CreateEntry();

        var nonUtcDate = new DateTimeOffset(
            2026,
            8,
            7,
            16,
            0,
            0,
            TimeSpan.FromHours(3));

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                entry.ChangeStatus(
                    LibraryEntryStatus.Reading,
                    nonUtcDate));

        Assert.Equal("updatedAtUtc", exception.ParamName);
    }

    [Fact]
    public void ChangeStatus_ShouldThrowArgumentOutOfRangeException_WhenUpdatedAtPrecedesCreatedAt()
    {
        LibraryEntry entry = CreateEntry();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            entry.ChangeStatus(
                LibraryEntryStatus.Reading,
                CreatedAtUtc.AddMinutes(-1)));
    }

    private static LibraryEntry CreateEntry()
    {
        return LibraryEntry.Create(
            UserId,
            BookId,
            LibraryEntryStatus.WantToRead,
            CreatedAtUtc);
    }
}