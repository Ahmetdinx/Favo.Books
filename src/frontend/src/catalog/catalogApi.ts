import { createCatalogCache } from './catalogCache.ts'
import type {
    BookDetailsResponse,
    BookSummariesRequest,
    BookSummariesResponse,
    BrowseBooksRequest,
    BrowseBooksResponse,
} from './catalogTypes'

export type CatalogCategory = { categoryId: string; name: string; slug: string; bookCount: number }
const pageCache = createCatalogCache<BrowseBooksResponse>()
const categoryCache = createCatalogCache<CatalogCategory[]>(1)

export async function getCatalogCategories(signal?: AbortSignal): Promise<CatalogCategory[]> {
    signal?.throwIfAborted()
    const cached = categoryCache.get('categories')
    if (cached) return cached
    const response = await fetch('/api/catalog/categories', { signal })
    if (!response.ok) throw new Error('Kitap türleri yüklenemedi.')
    const result = await response.json() as { items: CatalogCategory[] }
    if (!signal?.aborted) categoryCache.set('categories', result.items)
    return result.items
}

export async function browseBooks(
    request: BrowseBooksRequest,
    signal?: AbortSignal,
    refresh = false,
    authenticatedFetch?: (input: string | URL, init?: RequestInit) => Promise<Response>,
): Promise<BrowseBooksResponse> {
    const parameters = new URLSearchParams()

    const query = request.query.trim()
    const category = request.category.trim()

    if (query) {
        parameters.set('query', query)
    }

    parameters.set(
        'searchField',
        request.searchField,
    )

    if (category) {
        parameters.set('category', category)
    }

    parameters.set(
        'page',
        request.page.toString(),
    )

    parameters.set(
        'pageSize',
        request.pageSize.toString(),
    )

    parameters.set('sort', request.sort)
    signal?.throwIfAborted()
    const cacheKey = parameters.toString()
    const cached = refresh || authenticatedFetch ? undefined : pageCache.get(cacheKey)
    if (cached) return cached

    const response = await (authenticatedFetch ?? fetch)(
        `/api/catalog/books?${parameters.toString()}`,
        {
            signal,
            headers: {
                Accept: 'application/json',
            },
        },
    )

    if (!response.ok) {
        throw new Error(
            `Catalog request failed: ${response.status}`,
        )
    }

    const result = await response.json() as BrowseBooksResponse
    if (!signal?.aborted && !authenticatedFetch) pageCache.set(cacheKey, result)
    return result
}

export async function getBookById(
    bookId: string,
    signal?: AbortSignal,
): Promise<BookDetailsResponse | null> {
    const response = await fetch(
        `/api/catalog/books/${encodeURIComponent(bookId)}`,
        {
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
        throw new Error(
            `Book details request failed: ${response.status}`,
        )
    }

    return (await response.json()) as
        BookDetailsResponse
}

export async function getBookSummaries(
    request: BookSummariesRequest,
    signal?: AbortSignal,
): Promise<BookSummariesResponse> {
    if (request.bookIds.length === 0) {
        return {
            items: [],
        }
    }

    const response = await fetch(
        '/api/catalog/books/summaries',
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
        throw new Error(
            'Book summaries request failed: ' +
            response.status,
        )
    }

    return (await response.json()) as
        BookSummariesResponse
}
