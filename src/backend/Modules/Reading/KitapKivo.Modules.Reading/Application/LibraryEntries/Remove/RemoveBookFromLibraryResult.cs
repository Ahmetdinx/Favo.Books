namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Remove;

internal enum RemoveBookFromLibraryError
{
    None = 0,
    NotFound = 1
}

internal sealed record RemoveBookFromLibraryResult(
    RemoveBookFromLibraryError Error)
{
    public bool IsSuccess =>
        Error == RemoveBookFromLibraryError.None;

    public static RemoveBookFromLibraryResult Success()
    {
        return new RemoveBookFromLibraryResult(
            RemoveBookFromLibraryError.None);
    }

    public static RemoveBookFromLibraryResult NotFound()
    {
        return new RemoveBookFromLibraryResult(
            RemoveBookFromLibraryError.NotFound);
    }
}