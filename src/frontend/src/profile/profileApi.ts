import type {
    BedsideBookSelection,
    GetInfluentialBooksResponse,
    SetBedsideBookRequest,
    SetBedsideBookResponse,
    SetInfluentialBookRequest,
    SetInfluentialBookResponse,
} from './profileTypes'

export type AuthenticatedFetch = (
    input: string | URL,
    init?: RequestInit,
) => Promise<Response>

export class ProfileApiError extends Error {
    readonly status: number

    constructor(
        message: string,
        status: number,
    ) {
        super(message)

        this.name = 'ProfileApiError'
        this.status = status
    }
}

function throwRequestError(
    operation: string,
    response: Response,
): never {
    throw new ProfileApiError(
        `${operation} failed: ${response.status}`,
        response.status,
    )
}

export async function getBedsideBook(
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
): Promise<BedsideBookSelection | null> {
    const response = await authenticatedFetch(
        '/api/profiles/me/bedside-book',
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
            'Get bedside book request',
            response,
        )
    }

    return (await response.json()) as
        BedsideBookSelection
}

export async function setBedsideBook(
    authenticatedFetch: AuthenticatedFetch,
    request: SetBedsideBookRequest,
    signal?: AbortSignal,
): Promise<SetBedsideBookResponse> {
    const response = await authenticatedFetch(
        '/api/profiles/me/bedside-book',
        {
            method: 'PUT',
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
            'Set bedside book request',
            response,
        )
    }

    return (await response.json()) as
        SetBedsideBookResponse
}

export async function removeBedsideBook(
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
): Promise<void> {
    const response = await authenticatedFetch(
        '/api/profiles/me/bedside-book',
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
            'Remove bedside book request',
            response,
        )
    }
}

export async function getInfluentialBooks(
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
): Promise<GetInfluentialBooksResponse> {
    const response = await authenticatedFetch(
        '/api/profiles/me/influential-books',
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
            'Get influential books request',
            response,
        )
    }

    return (await response.json()) as
        GetInfluentialBooksResponse
}

export async function setInfluentialBook(
    authenticatedFetch: AuthenticatedFetch,
    position: number,
    request: SetInfluentialBookRequest,
    signal?: AbortSignal,
): Promise<SetInfluentialBookResponse> {
    const response = await authenticatedFetch(
        '/api/profiles/me/' +
        'influential-books/' +
        encodeURIComponent(
            position.toString(),
        ),
        {
            method: 'PUT',
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
            'Set influential book request',
            response,
        )
    }

    return (await response.json()) as
        SetInfluentialBookResponse
}

export async function removeInfluentialBook(
    authenticatedFetch: AuthenticatedFetch,
    position: number,
    signal?: AbortSignal,
): Promise<void> {
    const response = await authenticatedFetch(
        '/api/profiles/me/' +
        'influential-books/' +
        encodeURIComponent(
            position.toString(),
        ),
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
            'Remove influential book request',
            response,
        )
    }
}