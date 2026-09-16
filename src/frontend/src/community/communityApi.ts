import type { BookRatingSummary, RatingScores } from './communityTypes'

type AuthenticatedFetch = (
    input: string | URL,
    init?: RequestInit,
) => Promise<Response>

export class RatingApiError extends Error {
    readonly status: number

    constructor(status: number) {
        super(`Rating request failed: ${status}`)
        this.status = status
        this.name = 'RatingApiError'
    }
}

async function parse(response: Response) {
    if (!response.ok) throw new RatingApiError(response.status)
    return (await response.json()) as BookRatingSummary
}

export async function getBookRatings(
    bookId: string,
    authenticatedFetch: AuthenticatedFetch | null,
    signal?: AbortSignal,
) {
    const request = authenticatedFetch ?? fetch
    return parse(await request(
        `/api/community/books/${encodeURIComponent(bookId)}/ratings`,
        { signal, cache: 'no-store', headers: { Accept: 'application/json' } },
    ))
}

export async function setBookRating(
    bookId: string,
    scores: RatingScores,
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
) {
    return parse(await authenticatedFetch(
        `/api/community/books/${encodeURIComponent(bookId)}/rating`,
        {
            method: 'PUT',
            signal,
            headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
            body: JSON.stringify(scores),
        },
    ))
}

export async function removeBookRating(
    bookId: string,
    authenticatedFetch: AuthenticatedFetch,
    signal?: AbortSignal,
) {
    const response = await authenticatedFetch(
        `/api/community/books/${encodeURIComponent(bookId)}/rating`,
        { method: 'DELETE', signal },
    )
    // An already removed vote is the desired state; reload its summary.
    if (!response.ok && response.status !== 404) throw new RatingApiError(response.status)
}
