using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

namespace KitapKivo.Modules.Profiles.UnitTests
    .Domain.InfluentialBooks;

public sealed class InfluentialBookTests
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
        Create_ShouldCreateInfluentialBook_WhenValuesAreValid()
    {
        // Act
        InfluentialBook influentialBook =
            InfluentialBook.Create(
                UserId,
                BookId,
                5,
                "  Dünyaya bakışımı " +
                "değiştiren bir kitap.  ",
                CreatedAtUtc);

        // Assert
        Assert.NotEqual(
            Guid.Empty,
            influentialBook.Id);

        Assert.Equal(
            UserId,
            influentialBook.UserId);

        Assert.Equal(
            BookId,
            influentialBook.BookId);

        Assert.Equal(
            5,
            influentialBook.Position);

        Assert.Equal(
            "Dünyaya bakışımı " +
            "değiştiren bir kitap.",
            influentialBook
                .PersonalReflection);

        Assert.Equal(
            CreatedAtUtc,
            influentialBook.CreatedAtUtc);

        Assert.Equal(
            CreatedAtUtc,
            influentialBook.UpdatedAtUtc);
    }

    [Fact]
    public void
        Create_ShouldThrowArgumentOutOfRangeException_WhenPositionIsBelowMinimum()
    {
        // Act
        ArgumentOutOfRangeException exception =
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    InfluentialBook.Create(
                        UserId,
                        BookId,
                        0,
                        null,
                        CreatedAtUtc));

        // Assert
        Assert.Equal(
            "position",
            exception.ParamName);
    }

    [Fact]
    public void
        Create_ShouldThrowArgumentOutOfRangeException_WhenPositionExceedsMaximum()
    {
        // Act
        ArgumentOutOfRangeException exception =
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    InfluentialBook.Create(
                        UserId,
                        BookId,
                        6,
                        null,
                        CreatedAtUtc));

        // Assert
        Assert.Equal(
            "position",
            exception.ParamName);
    }

    [Fact]
    public void
        Create_ShouldThrowArgumentException_WhenReflectionExceedsLimit()
    {
        // Arrange
        string personalReflection =
            new(
                'a',
                InfluentialBook
                    .MaximumPersonalReflectionLength +
                1);

        // Act
        ArgumentException exception =
            Assert.Throws<
                ArgumentException>(
                () =>
                    InfluentialBook.Create(
                        UserId,
                        BookId,
                        1,
                        personalReflection,
                        CreatedAtUtc));

        // Assert
        Assert.Equal(
            "personalReflection",
            exception.ParamName);
    }

    [Fact]
    public void
        Change_ShouldUpdateBookPositionReflectionAndTimestamp()
    {
        // Arrange
        InfluentialBook influentialBook =
            InfluentialBook.Create(
                UserId,
                BookId,
                1,
                "İlk düşüncem",
                CreatedAtUtc);

        DateTimeOffset updatedAtUtc =
            CreatedAtUtc.AddHours(3);

        // Act
        influentialBook.Change(
            SecondBookId,
            4,
            "  Bende uzun süre kalan " +
            "yeni düşüncem.  ",
            updatedAtUtc);

        // Assert
        Assert.Equal(
            SecondBookId,
            influentialBook.BookId);

        Assert.Equal(
            4,
            influentialBook.Position);

        Assert.Equal(
            "Bende uzun süre kalan " +
            "yeni düşüncem.",
            influentialBook
                .PersonalReflection);

        Assert.Equal(
            updatedAtUtc,
            influentialBook.UpdatedAtUtc);
    }
}