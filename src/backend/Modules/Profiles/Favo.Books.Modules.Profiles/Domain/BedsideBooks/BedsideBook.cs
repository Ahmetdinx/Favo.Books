using Favo.Books.Modules.Profiles.Domain;

namespace Favo.Books.Modules.Profiles
    .Domain.BedsideBooks;

internal sealed class BedsideBook
{
    internal const int
        MaximumPersonalNoteLength = 500;

    private BedsideBook()
    {
    }

    private BedsideBook(
        Guid id,
        Guid userId,
        Guid bookId,
        string? personalNote,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        BookId = bookId;
        PersonalNote = personalNote;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid BookId { get; private set; }

    public string? PersonalNote
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

    public static BedsideBook Create(
        Guid userId,
        Guid bookId,
        string? personalNote,
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

        string? normalizedNote =
            ProfileBookGuard
                .NormalizeOptionalText(
                    personalNote,
                    MaximumPersonalNoteLength,
                    nameof(personalNote));

        ProfileBookGuard.EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        return new BedsideBook(
            Guid.NewGuid(),
            userId,
            bookId,
            normalizedNote,
            createdAtUtc);
    }

    public void Change(
        Guid bookId,
        string? personalNote,
        DateTimeOffset updatedAtUtc)
    {
        ProfileBookGuard
            .EnsureIdentifierIsNotEmpty(
                bookId,
                nameof(bookId));

        string? normalizedNote =
            ProfileBookGuard
                .NormalizeOptionalText(
                    personalNote,
                    MaximumPersonalNoteLength,
                    nameof(personalNote));

        ProfileBookGuard.EnsureUtc(
            updatedAtUtc,
            nameof(updatedAtUtc));

        ProfileBookGuard
            .EnsureUpdateDoesNotPrecedeCreation(
                updatedAtUtc,
                CreatedAtUtc,
                nameof(updatedAtUtc));

        if (BookId == bookId &&
            PersonalNote == normalizedNote)
        {
            return;
        }

        BookId = bookId;
        PersonalNote = normalizedNote;
        UpdatedAtUtc = updatedAtUtc;
    }
}