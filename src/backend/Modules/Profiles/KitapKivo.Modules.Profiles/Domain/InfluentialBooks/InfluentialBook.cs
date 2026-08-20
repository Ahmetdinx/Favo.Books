using KitapKivo.Modules.Profiles.Domain;

namespace KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

internal sealed class InfluentialBook
{
    internal const int MinimumPosition = 1;
    internal const int MaximumPosition = 5;
    internal const int
        MaximumPersonalReflectionLength = 1000;

    private InfluentialBook()
    {
    }

    private InfluentialBook(
        Guid id,
        Guid userId,
        Guid bookId,
        int position,
        string? personalReflection,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        BookId = bookId;
        Position = position;
        PersonalReflection =
            personalReflection;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid BookId { get; private set; }

    public int Position { get; private set; }

    public string? PersonalReflection
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset UpdatedAtUtc
    {
        get;
        private set;
    }

    public static InfluentialBook Create(
        Guid userId,
        Guid bookId,
        int position,
        string? personalReflection,
        DateTimeOffset createdAtUtc)
    {
        ProfileBookGuard
            .EnsureIdentifierIsNotEmpty(
                userId,
                nameof(userId));

        ProfileBookGuard
            .EnsureIdentifierIsNotEmpty(
                bookId,
                nameof(bookId));

        ProfileBookGuard
            .EnsurePositionIsInRange(
                position,
                MinimumPosition,
                MaximumPosition,
                nameof(position));

        string? normalizedReflection =
            ProfileBookGuard
                .NormalizeOptionalText(
                    personalReflection,
                    MaximumPersonalReflectionLength,
                    nameof(personalReflection));

        ProfileBookGuard.EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        return new InfluentialBook(
            Guid.NewGuid(),
            userId,
            bookId,
            position,
            normalizedReflection,
            createdAtUtc);
    }

    public void Change(
        Guid bookId,
        int position,
        string? personalReflection,
        DateTimeOffset updatedAtUtc)
    {
        ProfileBookGuard
            .EnsureIdentifierIsNotEmpty(
                bookId,
                nameof(bookId));

        ProfileBookGuard
            .EnsurePositionIsInRange(
                position,
                MinimumPosition,
                MaximumPosition,
                nameof(position));

        string? normalizedReflection =
            ProfileBookGuard
                .NormalizeOptionalText(
                    personalReflection,
                    MaximumPersonalReflectionLength,
                    nameof(personalReflection));

        ProfileBookGuard.EnsureUtc(
            updatedAtUtc,
            nameof(updatedAtUtc));

        ProfileBookGuard
            .EnsureUpdateDoesNotPrecedeCreation(
                updatedAtUtc,
                CreatedAtUtc,
                nameof(updatedAtUtc));

        if (BookId == bookId &&
            Position == position &&
            PersonalReflection ==
                normalizedReflection)
        {
            return;
        }

        BookId = bookId;
        Position = position;
        PersonalReflection =
            normalizedReflection;
        UpdatedAtUtc = updatedAtUtc;
    }
}