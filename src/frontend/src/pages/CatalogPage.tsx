import {
    useEffect,
    useRef,
    useState,
    type SyntheticEvent,
} from 'react'

import {
    Link,
    useSearchParams,
} from 'react-router'

import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { browseBooks, getCatalogCategories, type CatalogCategory } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'
import BookObject from '../components/BookObject'
import { useCatalogLibrary } from '../reading/useCatalogLibrary'
import CatalogLibraryActions from '../reading/CatalogLibraryActions'

import type {
    BrowseBooksResponse,
    CatalogBookSort,
    CatalogSearchField,
} from '../catalog/catalogTypes'

const PAGE_SIZE = 12

function parsePage(value: string | null) {
    const parsedValue = Number.parseInt(
        value ?? '',
        10,
    )

    return Number.isInteger(parsedValue) &&
        parsedValue > 0
        ? parsedValue
        : 1
}

function parseSearchField(
    value: string | null,
): CatalogSearchField {
    return value === 'title' ||
        value === 'author'
        ? value
        : 'all'
}

function parseSort(
    value: string | null,
): CatalogBookSort {
    return value === 'title' ||
        value === 'first-published-newest'
        ? value
        : 'recently-added'
}

function CatalogPage() {
    const { session } = useAuth()
    const authFetch = useAuthenticatedFetch()
    const [
        searchParameters,
        setSearchParameters,
    ] = useSearchParams()

    const query =
        searchParameters.get('query')?.trim() ??
        ''

    const category =
        searchParameters
            .get('category')
            ?.trim() ?? ''

    const page = parsePage(
        searchParameters.get('page'),
    )

    const searchField = parseSearchField(
        searchParameters.get('searchField'),
    )

    const sort = parseSort(
        searchParameters.get('sort'),
    )

    const [
        draftQuery,
        setDraftQuery,
    ] = useState(query)

    const [
        draftSearchField,
        setDraftSearchField,
    ] = useState<CatalogSearchField>(
        searchField,
    )

    const [result, setResult] =
        useState<BrowseBooksResponse | null>(
            null,
        )

    const [categories, setCategories] = useState<CatalogCategory[]>([])
    const library = useCatalogLibrary(result?.items.map(book => book.bookId) ?? [])
    const [categoryError, setCategoryError] = useState(false)
    const [categoriesLoading, setCategoriesLoading] = useState(true)
    const [categoryRetry, setCategoryRetry] = useState(0)

    useEffect(() => {
        const controller = new AbortController()
        setCategoriesLoading(true)
        setCategoryError(false)
        void getCatalogCategories(controller.signal)
            .then(items => { if (!controller.signal.aborted) setCategories(items) })
            .catch(() => { if (!controller.signal.aborted) setCategoryError(true) })
            .finally(() => { if (!controller.signal.aborted) setCategoriesLoading(false) })
        return () => controller.abort()
    }, [categoryRetry])

    const [isLoading, setIsLoading] =
        useState(true)

    const [
        errorMessage,
        setErrorMessage,
    ] = useState('')

    const [
        requestVersion,
        setRequestVersion,
    ] = useState(0)
    const retriedVersion = useRef(0)
    useEffect(() => {
        const refresh = () => setRequestVersion(value => value + 1)
        window.addEventListener('library-changed', refresh)
        return () => window.removeEventListener('library-changed', refresh)
    }, [])

    useEffect(() => {
        setDraftQuery(query)
        setDraftSearchField(searchField)
    }, [query, searchField])

    useEffect(() => {
        const nextQuery = draftQuery.trim()
        if (nextQuery === query && draftSearchField === searchField) return

        const timer = window.setTimeout(() => {
            setSearchParameters(current => {
                const next = new URLSearchParams(current)
                if (nextQuery) next.set('query', nextQuery)
                else next.delete('query')
                next.set('searchField', draftSearchField)
                next.set('page', '1')
                return next
            }, { replace: true })
        }, 300)

        return () => window.clearTimeout(timer)
    }, [draftQuery, draftSearchField, query, searchField, setSearchParameters])

    useEffect(() => {
        const controller =
            new AbortController()

        async function loadBooks() {
            const refresh = retriedVersion.current !== requestVersion
            retriedVersion.current = requestVersion
            setIsLoading(true)
            setErrorMessage('')

            try {
                const response =
                    await browseBooks(
                        {
                            query,
                            searchField,
                            category,
                            page,
                            pageSize: PAGE_SIZE,
                            sort,
                        },
                        controller.signal,
                        refresh,
                        session ? authFetch : undefined,
                    )

                if (!controller.signal.aborted) setResult(response)
            } catch {
                if (
                    !controller.signal.aborted
                ) {
                    setErrorMessage(
                        'Kitaplar yüklenemedi. ' +
                        'Lütfen biraz sonra tekrar deneyin.',
                    )
                }
            } finally {
                if (
                    !controller.signal.aborted
                ) {
                    setIsLoading(false)
                }
            }
        }

        void loadBooks()

        return () => {
            controller.abort()
        }
    }, [
        authFetch, session,
        category,
        page,
        query,
        requestVersion,
        searchField,
        sort,
    ])

    function updateSearchParameters(
        changes: Record<
            string,
            string | null
        >,
    ) {
        setSearchParameters((current) => {
            const next =
                new URLSearchParams(current)

            Object.entries(changes)
                .forEach(([key, value]) => {
                    if (value) {
                        next.set(key, value)
                    } else {
                        next.delete(key)
                    }
                })

            return next
        })
    }

    function handleSearch(
        event: SyntheticEvent<HTMLFormElement>,
    ) {
        event.preventDefault()

        updateSearchParameters({
            query: draftQuery.trim() || null,
            searchField: draftSearchField,
            page: '1',
        })
    }

    function handleSortChange(
        selectedSort: CatalogBookSort,
    ) {
        updateSearchParameters({
            sort: selectedSort,
            page: '1',
        })
    }

    function handleCategoryChange(
        selectedCategory: string,
    ) {
        updateSearchParameters({
            category: selectedCategory,
            page: '1',
        })
    }

    function handlePageChange(
        nextPage: number,
    ) {
        updateSearchParameters({
            page: nextPage.toString(),
        })

        window.scrollTo({
            top: 0,
            behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
        })
    }

    const visibleCategories = categories.filter(item => item.bookCount > 0 || item.slug === category)
    const hasFilters = !!query || !!category

    return (
        <section className="catalog-page">
            <header className="catalog-hero">
                <div>
                    <h1>Katalog</h1>
                    <p>
                        {result
                            ? `${result.totalCount.toLocaleString('tr-TR')} roman ${hasFilters ? 'bulundu' : 'rafta'}`
                            : 'Raflar hazırlanıyor…'}
                    </p>
                </div>

                <form className="catalog-search" role="search" onSubmit={handleSearch}>
                    <label className="catalog-search-box">
                        <span className="profile-visually-hidden">Kitap, yazar veya ISBN ara</span>
                        <input
                            type="search"
                            maxLength={200}
                            placeholder="Kitap, yazar veya ISBN ara"
                            value={draftQuery}
                            onChange={(event) => setDraftQuery(event.target.value)}
                        />
                    </label>
                    <button className="button primary-button" type="submit">Ara</button>
                </form>
            </header>

            <div className="catalog-filters">
                <div className="catalog-genres" role="group" aria-label="Roman türleri">
                    <button type="button" aria-pressed={!category} disabled={categoriesLoading}
                        onClick={() => handleCategoryChange('')}>Tüm türler</button>
                    {visibleCategories.map(item => (
                        <button type="button" key={item.categoryId} aria-pressed={item.slug === category}
                            onClick={() => handleCategoryChange(item.slug)}>
                            {item.name}<small>{item.bookCount}</small>
                        </button>
                    ))}
                </div>

                <div className="catalog-controls">
                    <label>
                        <span>Ara:</span>
                        <select aria-label="Arama alanı" value={draftSearchField}
                            onChange={(event) => setDraftSearchField(event.target.value as CatalogSearchField)}>
                            <option value="all">Tümü</option>
                            <option value="title">Kitap adı</option>
                            <option value="author">Yazar</option>
                        </select>
                    </label>
                    <label>
                        <span>Sırala:</span>
                        <select aria-label="Sıralama" value={sort}
                            onChange={(event) => handleSortChange(event.target.value as CatalogBookSort)}>
                            <option value="recently-added">Son eklenenler</option>
                            <option value="title">Kitap adına göre</option>
                            <option value="first-published-newest">En yeni yayınlanan</option>
                        </select>
                    </label>
                </div>
            </div>

            {categoryError && <p className="catalog-category-error" role="alert">
                Roman türleri yüklenemedi. <button type="button"
                    onClick={() => setCategoryRetry(value => value + 1)}>Tekrar dene</button>
            </p>}

            <div className="catalog-results" aria-busy={isLoading}>
                <div className="catalog-update-status" role="status">{isLoading && result ? 'Sonuçlar güncelleniyor…' : ''}</div>

                {isLoading && !result && (
                    <div className="book-leaves" role="status" aria-label="Kitaplar yükleniyor">
                        {Array.from({ length: 6 }, (_, index) => <div className="book-leaf book-leaf-skeleton" key={index} />)}
                    </div>
                )}

                {errorMessage && !isLoading && (
                    <div className="catalog-status catalog-error" role="alert">
                        <p>{errorMessage}</p>
                        <button className="button primary-button" type="button"
                            onClick={() => setRequestVersion((current) => current + 1)}>
                            Tekrar dene
                        </button>
                    </div>
                )}

                {result && result.items.length === 0 && (
                    <div className="catalog-empty">
                        <h2>{query ? `“${query}” için sonuç yok` : 'Bu rafta kitap yok'}</h2>
                        <p>Yazımı değiştirmeyi ya da yalnızca yazarın soyadını aramayı deneyebilirsin.</p>
                        {hasFilters && <button className="button secondary-button" type="button"
                            onClick={() => updateSearchParameters({ query: null, category: null, page: '1' })}>
                            Filtreleri temizle
                        </button>}
                    </div>
                )}

                {result && result.items.length > 0 && (
                    <>
                        <div className="book-leaves" data-updating={isLoading}>
                            {result.items.map((book, index) => {
                                const title = cleanBookTitle(book.title)
                                const authors = book.authors.map(author => author.name).join(', ')
                                return (
                                    <article className="book-leaf" key={book.bookId}>
                                        <div className="book-leaf-body">
                                            <Link to={`/catalog/books/${book.bookId}`} aria-label={`${title} detaylarını görüntüle`}>
                                                <BookObject title={title} author={authors} coverImageUrl={book.coverImageUrl} priority={index < 3} />
                                            </Link>
                                            <div className="book-leaf-copy">
                                                {book.firstPublishedYear && <p className="book-leaf-year">{book.firstPublishedYear}</p>}
                                                <h2><Link to={`/catalog/books/${book.bookId}`}>{title}</Link></h2>
                                                <p className="book-leaf-author">{authors || 'Yazar bilgisi bulunmuyor'}</p>
                                                {book.categories.length > 0 && (
                                                    <div className="book-leaf-genres">
                                                        {book.categories.map(bookCategory => (
                                                            <button key={bookCategory.categoryId} type="button"
                                                                onClick={() => handleCategoryChange(bookCategory.slug)}>
                                                                {bookCategory.name}
                                                            </button>
                                                        ))}
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                        <CatalogLibraryActions bookId={book.bookId} title={title} library={library} />
                                        <p className="book-leaf-number" aria-hidden="true">— {(result.page - 1) * result.pageSize + index + 1} —</p>
                                    </article>
                                )
                            })}
                        </div>

                        {result.totalPages > 1 && (
                            <nav className="catalog-pagination" aria-label="Katalog sayfaları">
                                <button className="button secondary-button" type="button"
                                    disabled={isLoading || result.page <= 1}
                                    onClick={() => handlePageChange(result.page - 1)}>
                                    Önceki
                                </button>
                                <span>Sayfa <strong>{result.page}</strong> / {result.totalPages}</span>
                                <button className="button secondary-button" type="button"
                                    disabled={isLoading || result.page >= result.totalPages}
                                    onClick={() => handlePageChange(result.page + 1)}>
                                    Sonraki
                                </button>
                            </nav>
                        )}
                    </>
                )}
            </div>
        </section>
    )
}

export default CatalogPage
