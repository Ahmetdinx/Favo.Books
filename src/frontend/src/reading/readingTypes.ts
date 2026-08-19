export type LibraryEntryStatus =
    | 'WantToRead'
    | 'Reading'
    | 'Read'
    | 'OnHold'
    | 'Dropped'

export type LibraryEntry = {
    libraryEntryId: string
    bookId: string
    status: LibraryEntryStatus
    createdAtUtc: string
    updatedAtUtc: string
}

export type AddBookToLibraryRequest = {
    bookId: string
    status: LibraryEntryStatus
}

export type AddBookToLibraryResponse = {
    libraryEntryId: string
}

export type UpdateLibraryStatusRequest = {
    status: LibraryEntryStatus
}

export type UpdateLibraryStatusResponse = {
    libraryEntryId: string
    status: LibraryEntryStatus
    updatedAtUtc: string
}