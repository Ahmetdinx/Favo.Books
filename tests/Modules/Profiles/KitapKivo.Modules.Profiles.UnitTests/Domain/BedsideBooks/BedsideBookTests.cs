using KitapKivo.Modules.Profiles
    .Domain.BedsideBooks;

namespace KitapKivo.Modules.Profiles.UnitTests
    .Domain.BedsideBooks;

public sealed class BedsideBookTests
{
    private static readonly Guid UserId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid BookId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid
        SecondBookId =
            Guid.Parse(
                "20000000-0000-0000-0000-000000000002");

    private static readonly DateTimeOffset
        CreatedAtUtc =
            new(
                2026,
                8,
                20,
                12,
                0,
                0,
                TimeSpan.Zero);

    [Fact]
    public void
        Create_ShouldCreateBedsideBook_WhenValuesAreValid()
    {
        // Act
        BedsideBook bedsideBook =
            BedsideBook.Create(
                UserId,
                BookId,
                "  Bana her okumada yeni " +
                "bir şey hatırlatıyor.  ",
                CreatedAtUtc);

        // Assert
        Assert.NotEqual(
            Guid.Empty,
            bedsideBook.Id);

        Assert.Equal(
            UserId,
            bedsideBook.UserId);

        Assert.Equal(
            BookId,
            bedsideBook.BookId);

        Assert.Equal(
            "Bana her okumada yeni " +
            "bir şey hatırlatıyor.",
            bedsideBook.PersonalNote);

        Assert.Equal(
            CreatedAtUtc,
            bedsideBook.CreatedAtUtc);

        Assert.Equal(
            CreatedAtUtc,
            bedsideBook.UpdatedAtUtc);
    }

    [Fact]
    public void
        Create_ShouldSetPersonalNoteToNull_WhenNoteIsWhitespace()
    {
        // Act
        BedsideBook bedsideBook =
            BedsideBook.Create(
                UserId,
                BookId,
                "   ",
                CreatedAtUtc);

        // Assert
        Assert.Null(
            bedsideBook.PersonalNote);
    }

    [Fact]
    public void
        Create_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        // Act
        ArgumentException exception =
            Assert.Throws<
                ArgumentException>(
                () =>
                    BedsideBook.Create(
                        Guid.Empty,
                        BookId,
                        null,
                        CreatedAtUtc));

        // Assert
        Assert.Equal(
            "userId",
            exception.ParamName);
    }

    [Fact]
    public void
        Create_ShouldThrowArgumentException_WhenPersonalNoteExceedsLimit()
    {
        // Arrange
        string personalNote =
            new(
                'a',
                BedsideBook
                    .MaximumPersonalNoteLength +
                1);

        // Act
        ArgumentException exception =
            Assert.Throws<
                ArgumentException>(
                () =>
                    BedsideBook.Create(
                        UserId,
                        BookId,
                        personalNote,
                        CreatedAtUtc));

        // Assert
        Assert.Equal(
            "personalNote",
            exception.ParamName);
    }

    [Fact]
    public void
        Change_ShouldUpdateBookNoteAndTimestamp()
    {
        // Arrange
        BedsideBook bedsideBook =
            BedsideBook.Create(
                UserId,
                BookId,
                "İlk not",
                CreatedAtUtc);

        DateTimeOffset updatedAtUtc =
            CreatedAtUtc.AddHours(2);

        // Act
        bedsideBook.Change(
            SecondBookId,
            "  Yeni başucu kitabım.  ",
            updatedAtUtc);

        // Assert
        Assert.Equal(
            SecondBookId,
            bedsideBook.BookId);

        Assert.Equal(
            "Yeni başucu kitabım.",
            bedsideBook.PersonalNote);

        Assert.Equal(
            updatedAtUtc,
            bedsideBook.UpdatedAtUtc);
    }
}