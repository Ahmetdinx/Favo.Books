namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;

internal enum AddBookToLibraryError
{
    None = 0,
    AlreadyExists = 1
}

internal sealed record AddBookToLibraryResult(
    Guid? LibraryEntryId,
    AddBookToLibraryError Error)
{
    public bool IsSuccess =>
        Error == AddBookToLibraryError.None;

    public static AddBookToLibraryResult Success(
        Guid libraryEntryId)
    {
        return new AddBookToLibraryResult(
            libraryEntryId,
            AddBookToLibraryError.None);
    }

    public static AddBookToLibraryResult AlreadyExists()
    {
        return new AddBookToLibraryResult(
            null,
            AddBookToLibraryError.AlreadyExists);
    }
}