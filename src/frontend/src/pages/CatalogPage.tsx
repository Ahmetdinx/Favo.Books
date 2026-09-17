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

import { browseBooks, getCatalogCategories, type CatalogCategory } from '../catalog/catalogApi'
import { catalogCoverUrl } from '../catalog/coverImage'

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

type BookCoverProps = {
    coverImageUrl: string | null
    title: string
    priority: boolean
}

function BookCover({
    coverImageUrl,
    title,
    priority,
}: BookCoverProps) {
    const [hasError, setHasError] =
        useState(false)

    useEffect(() => {
        setHasError(false)
    }, [coverImageUrl])

    if (!coverImageUrl || hasError) {
        return (
            <div
                className="catalog-cover-placeholder"
                aria-label={`${title} için kapak bulunamadı`}
            >
                <span>K</span>
                <small>Kapak bulunamadı</small>
            </div>
        )
    }

    return (
        <img
            className="catalog-cover-image"
            src={catalogCoverUrl(coverImageUrl)}
            alt={`${title} kitap kapağı`}
            width={124}
            height={184}
            loading={priority ? 'eager' : 'lazy'}
            fetchPriority={priority ? 'high' : 'auto'}
            decoding="async"
            onError={() => setHasError(true)}
        />
    )
}

function CatalogPage() {
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
        setDraftQuery(query)
        setDraftSearchField(searchField)
    }, [query, searchField])

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

    return (
        <section className="catalog-page">
            <header className="catalog-page-header">
                <div>
                    <p className="eyebrow">
                        Kitapları keşfet
                    </p>

                    <h1>Katalog</h1>

                    <p>
                        Kitaplar arasında arama yap,
                        kapakları incele ve sana uygun
                        kitabı keşfet.
                    </p>
                </div>

                {result && (
                    <div className="catalog-count">
                        <strong>
                            {result.totalCount}
                        </strong>

                        <span>kitap bulundu</span>
                    </div>
                )}
            </header>

            <form
                className="catalog-toolbar"
                onSubmit={handleSearch}
            >
                <label className="catalog-field catalog-search-field">
                    <span>Arama</span>

                    <input
                        type="search"
                        maxLength={200}
                        placeholder="Kitap veya yazar ara..."
                        value={draftQuery}
                        onChange={(event) =>
                            setDraftQuery(
                                event.target.value,
                            )
                        }
                    />
                </label>

                <label className="catalog-field">
                    <span>Arama alanı</span>

                    <select
                        value={draftSearchField}
                        onChange={(event) =>
                            setDraftSearchField(
                                event.target
                                    .value as
                                CatalogSearchField,
                            )
                        }
                    >
                        <option value="all">
                            Tümü
                        </option>

                        <option value="title">
                            Kitap adı
                        </option>

                        <option value="author">
                            Yazar
                        </option>
                    </select>
                </label>

                <label className="catalog-field">
                    <span>Kitap türleri</span>
                    <select value={category} disabled={categoriesLoading || categoryError}
                        onChange={event => handleCategoryChange(event.target.value)}>
                        <option value="">{categoriesLoading ? 'Türler yükleniyor…' : 'Tüm türler'}</option>
                        {category && !categories.some(item => item.slug === category) &&
                            <option value={category}>{category}</option>}
                        {categories.map(item => <option key={item.categoryId} value={item.slug}>
                            {item.name} ({item.bookCount})
                        </option>)}
                    </select>
                </label>

                <label className="catalog-field">
                    <span>Sıralama</span>

                    <select
                        value={sort}
                        onChange={(event) =>
                            handleSortChange(
                                event.target
                                    .value as
                                CatalogBookSort,
                            )
                        }
                    >
                        <option value="recently-added">
                            Son eklenenler
                        </option>

                        <option value="title">
                            Kitap adına göre
                        </option>

                        <option value="first-published-newest">
                            En yeni yayınlanan
                        </option>
                    </select>
                </label>

                <button
                    className="button primary-button catalog-search-button"
                    type="submit"
                >
                    Ara
                </button>
            </form>

            {categoryError && <p className="catalog-category-error" role="alert">
                Kitap türleri yüklenemedi. <button type="button"
                    onClick={() => setCategoryRetry(value => value + 1)}>Tekrar dene</button>
            </p>}

            {category && (
                <div className="catalog-active-filter">
                    <span>
                        Kitap türü:
                        <strong>{categories.find(item => item.slug === category)?.name ?? category}</strong>
                    </span>

                    <button
                        type="button"
                        onClick={() =>
                            updateSearchParameters({
                                category: null,
                                page: '1',
                            })
                        }
                    >
                        Filtreyi kaldır
                    </button>
                </div>
            )}

            {isLoading && (
                <div
                    className="catalog-status"
                    role="status"
                >
                    Kitaplar yükleniyor...
                </div>
            )}

            {errorMessage && !isLoading && (
                <div
                    className="catalog-status catalog-error"
                    role="alert"
                >
                    <p>{errorMessage}</p>

                    <button
                        className="button primary-button"
                        type="button"
                        onClick={() =>
                            setRequestVersion(
                                (current) =>
                                    current + 1,
                            )
                        }
                    >
                        Tekrar Dene
                    </button>
                </div>
            )}

            {result &&
                !isLoading &&
                !errorMessage &&
                result.items.length === 0 && (
                    <div className="catalog-status">
                        <strong>
                            Sonuç bulunamadı
                        </strong>

                        <p>
                            Arama kelimelerini veya
                            filtreleri değiştirerek
                            tekrar deneyin.
                        </p>
                    </div>
                )}

            {result &&
                !isLoading &&
                !errorMessage &&
                result.items.length > 0 && (
                    <>
                        <div className="catalog-grid">
                            {result.items.map(
                                (book, index) => (
                                    <article
                                        className="catalog-book-card"
                                        key={book.bookId}
                                    >
                                        <Link
                                            className="catalog-cover-link"
                                            to={`/catalog/books/${book.bookId}`}
                                            aria-label={`${book.title} detaylarını görüntüle`}
                                        >
                                            <div className="catalog-cover">
                                                <BookCover
                                                    coverImageUrl={
                                                        book.coverImageUrl
                                                    }
                                                    title={book.title}
                                                    priority={index < 2}
                                                />
                                            </div>
                                        </Link>

                                        <div className="catalog-book-content">
                                            <p className="catalog-book-year">
                                                {book.firstPublishedYear
                                                    ? `İlk yayın: ${book.firstPublishedYear}`
                                                    : 'Yayın yılı bilinmiyor'}
                                            </p>

                                            <h2>
                                                <Link
                                                    className="catalog-book-title-link"
                                                    to={`/catalog/books/${book.bookId}`}
                                                >
                                                    {book.title}
                                                </Link>
                                            </h2>

                                            <p className="catalog-book-authors">
                                                {book.authors
                                                    .length >
                                                    0
                                                    ? book.authors
                                                        .map(
                                                            (
                                                                author,
                                                            ) =>
                                                                author.name,
                                                        )
                                                        .join(
                                                            ', ',
                                                        )
                                                    : 'Yazar bilgisi bulunmuyor'}
                                            </p>

                                            {book.categories
                                                .length >
                                                0 && (
                                                    <div className="catalog-categories">
                                                        {book.categories.map(
                                                            (
                                                                bookCategory,
                                                            ) => (
                                                                <button
                                                                    key={
                                                                        bookCategory.categoryId
                                                                    }
                                                                    type="button"
                                                                    onClick={() =>
                                                                        handleCategoryChange(
                                                                            bookCategory.slug,
                                                                        )
                                                                    }
                                                                >
                                                                    {
                                                                        bookCategory.name
                                                                    }
                                                                </button>
                                                            ),
                                                        )}
                                                    </div>
                                                )}
                                        </div>
                                        <Link
                                            className="catalog-detail-link"
                                            to={`/catalog/books/${book.bookId}`}
                                        >
                                            Detayları Gör
                                        </Link>
                                    </article>
                                ),
                            )}
                        </div>

                        {result.totalPages > 1 && (
                            <nav
                                className="catalog-pagination"
                                aria-label="Katalog sayfaları"
                            >
                                <button
                                    className="button secondary-button"
                                    type="button"
                                    disabled={
                                        result.page <= 1
                                    }
                                    onClick={() =>
                                        handlePageChange(
                                            result.page -
                                            1,
                                        )
                                    }
                                >
                                    Önceki
                                </button>

                                <span>
                                    Sayfa{' '}
                                    <strong>
                                        {result.page}
                                    </strong>{' '}
                                    /{' '}
                                    {result.totalPages}
                                </span>

                                <button
                                    className="button secondary-button"
                                    type="button"
                                    disabled={
                                        result.page >=
                                        result.totalPages
                                    }
                                    onClick={() =>
                                        handlePageChange(
                                            result.page +
                                            1,
                                        )
                                    }
                                >
                                    Sonraki
                                </button>
                            </nav>
                        )}
                    </>
                )}
        </section>
    )
}

export default CatalogPage
