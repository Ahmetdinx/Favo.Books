import type {
    AddBookToLibraryRequest,
    AddBookToLibraryResponse,
    LibraryEntry,
    LibraryEntryStatus,
    LibraryPageRequest,
    LibraryPageResponse,
    UpdateLibraryStatusResponse,
    LibrarySummaryResponse,
} from './readingTypes'

export type AuthenticatedFetch = (
    input: string | URL,
    init?: RequestInit,
) => Promise<Response>

export class ReadingApiError extends Error {
    readonly status: number

    constructor(
        message: string,
        status: number,
    ) {
        super(message)

        this.name = 'ReadingApiError'
        this.status = status
    }
}

function throwRequestError(
    operation: string,
    response: Response,
): never {
    throw new ReadingApiError(
        `${operation} failed: ${response.status}`,
        response.status,
    )
}

export async function getLibraryEntryByBook(
    authenticatedFetch: AuthenticatedFetch,
    bookId: string,
    signal?: AbortSignal,
): Promise<LibraryEntry | null> {
    const response = await authenticatedFetch(
        '/api/reading/library/books/' +
        encodeURIComponent(bookId),
        {
            method: 'GET',
            signal,
            headers: {
                Accept: 'application/json',
            },
        },
    )

    if (response.status === 404) {
        return null
    }

    if (!response.ok) {
        throwRequestError(
            'Get library entry by book request',
            response,
        )
    }

    return (await response.json()) as
        LibraryEntry
}

export async function addBookToLibrary(
    authenticatedFetch: AuthenticatedFetch,
    request: AddBookToLibraryRequest,
    signal?: AbortSignal,
): Promise<AddBookToLibraryResponse> {
    const response = await authenticatedFetch(
        '/api/reading/library',
        {
            method: 'POST',
            signal,
            headers: {
                Accept: 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        },
    )

    if (!response.ok) {
        throwRequestError(
            'Add book to library request',
            response,
        )
    }

    return (await response.json()) as
        AddBookToLibraryResponse
}

export async function updateLibraryStatus(
    authenticatedFetch: AuthenticatedFetch,
    libraryEntryId: string,
    status: LibraryEntryStatus,
    signal?: AbortSignal,
): Promise<UpdateLibraryStatusResponse> {
    const response = await authenticatedFetch(
        '/api/reading/library/' +
        encodeURIComponent(libraryEntryId) +
        '/status',
        {
            method: 'PATCH',
            signal,
            headers: {
                Accept: 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                status,
            }),
        },
    )

    if (!response.ok) {
        throwRequestError(
            'Update library status request',
            response,
        )
    }

    return (await response.json()) as
        UpdateLibraryStatusResponse
}

export async function removeBookFromLibrary(
    authenticatedFetch: AuthenticatedFetch,
    libraryEntryId: string,
    signal?: AbortSignal,
): Promise<void> {
    const response = await authenticatedFetch(
        '/api/reading/library/' +
        encodeURIComponent(libraryEntryId),
        {
            method: 'DELETE',
            signal,
            headers: {
                Accept: 'application/json',
            },
        },
    )

    if (!response.ok) {
        throwRequestError(
            'Remove book from library request',
            response,
        )
    }


}

export async function getLibrary(
    authenticatedFetch: AuthenticatedFetch,
    request: LibraryPageRequest,
    signal?: AbortSignal,
): Promise<LibraryPageResponse> {
    const parameters = new URLSearchParams()

    if (request.status) {
        parameters.set(
            'status',
            request.status,
        )
    }

    parameters.set(
        'page',
        request.page.toString(),
    )

    parameters.set(
        'pageSize',
        request.pageSize.toString(),
    )

    const response = await authenticatedFetch(
        '/api/reading/library?' +
        parameters.toString(),
        {
            method: 'GET',
            signal,
            headers: {
                Accept: 'application/json',
            },
        },
    )

    if (!response.ok) {
        throwRequestError(
            'Get library request',
            response,
        )
    }

    return (await response.json()) as
        LibraryPageResponse
}

export async function getLibrarySummary(
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
): Promise<LibrarySummaryResponse> {
    const response = await authenticatedFetch(
        '/api/reading/library/summary',
        {
            method: 'GET',
            signal,
            headers: {
                Accept: 'application/json',
            },
        },
    )

    if (!response.ok) {
        throwRequestError(
            'Get library summary request',
            response,
        )
    }

    return (await response.json()) as
        LibrarySummaryResponse
}