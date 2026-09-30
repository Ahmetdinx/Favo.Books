namespace Favo.Books.Modules.Reading.Domain.LibraryEntries;

internal sealed class LibraryEntry
{
    private LibraryEntry()
    {
    }

    private LibraryEntry(
        Guid id,
        Guid userId,
        Guid bookId,
        LibraryEntryStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        BookId = bookId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid BookId { get; private set; }

    public LibraryEntryStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static LibraryEntry Create(
        Guid userId,
        Guid bookId,
        LibraryEntryStatus status,
        DateTimeOffset createdAtUtc)
    {
        EnsureIdentifierIsNotEmpty(
            userId,
            nameof(userId));

        EnsureIdentifierIsNotEmpty(
            bookId,
            nameof(bookId));

        EnsureStatusIsValid(status);

        EnsureUtc(
            createdAtUtc,
            nameof(createdAtUtc));

        return new LibraryEntry(
            Guid.NewGuid(),
            userId,
            bookId,
            status,
            createdAtUtc);
    }

    public void ChangeStatus(
        LibraryEntryStatus status,
        DateTimeOffset updatedAtUtc)
    {
        EnsureStatusIsValid(status);

        EnsureUtc(
            updatedAtUtc,
            nameof(updatedAtUtc));

        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "Güncellenme zamanı oluşturulma zamanından önce olamaz.");
        }

        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void EnsureIdentifierIsNotEmpty(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Kimlik boş olamaz.",
                parameterName);
        }
    }

    private static void EnsureStatusIsValid(
        LibraryEntryStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "Geçerli bir okuma durumu belirtilmelidir.");
        }
    }

    private static void EnsureUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Tarih UTC olmalıdır.",
                parameterName);
        }
    }
}