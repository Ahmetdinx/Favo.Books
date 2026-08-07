namespace KitapKivo.Modules.Reading.Domain.LibraryEntries;

internal enum LibraryEntryStatus
{
    WantToRead = 1,
    Reading = 2,
    Read = 3,
    OnHold = 4,
    Dropped = 5
}