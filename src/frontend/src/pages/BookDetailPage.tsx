import {
    useEffect,
    useState,
} from 'react'

import {
    Link,
    useParams,
} from 'react-router'

import { getBookById } from '../catalog/catalogApi'
import ReadingStatusPanel from '../reading/readingStatusPanel'
import BookProfileActionsPanel from '../profile/BookProfileActionsPanel'
import BookRatingPanel from '../community/BookRatingPanel'

import type {
    BookDetailsResponse,
    BookEdition,
} from '../catalog/catalogTypes'

type DetailCoverProps = {
    coverImageUrl: string | null
    title: string
}

function DetailCover({
    coverImageUrl,
    title,
}: DetailCoverProps) {
    const [hasError, setHasError] =
        useState(false)

    useEffect(() => {
        setHasError(false)
    }, [coverImageUrl])

    if (!coverImageUrl || hasError) {
        return (
            <div
                className="book-detail-cover-placeholder"
                aria-label={`${title} için kapak bulunamadı`}
            >
                <span>K</span>
                <small>Kapak bulunamadı</small>
            </div>
        )
    }

    return (
        <img
            className="book-detail-cover-image"
            src={coverImageUrl}
            alt={`${title} kitap kapağı`}
            onError={() => setHasError(true)}
        />
    )
}

function getLanguageName(
    languageCode: string,
) {
    const normalizedCode =
        languageCode.toLowerCase()

    if (
        normalizedCode === 'tur' ||
        normalizedCode === 'tr'
    ) {
        return 'Türkçe'
    }

    if (
        normalizedCode === 'eng' ||
        normalizedCode === 'en'
    ) {
        return 'İngilizce'
    }

    return languageCode.toUpperCase()
}

function getFormatName(format: string) {
    switch (format.toLowerCase()) {
        case 'hardcover':
            return 'Ciltli'

        case 'paperback':
            return 'Karton kapak'

        case 'ebook':
            return 'E-kitap'

        case 'audiobook':
            return 'Sesli kitap'

        case 'unknown':
            return 'Belirtilmemiş'

        default:
            return format
    }
}

function BookDetailPage() {
    const { bookId } = useParams<{
        bookId: string
    }>()

    const [book, setBook] =
        useState<BookDetailsResponse | null>(
            null,
        )

    const [isLoading, setIsLoading] =
        useState(true)

    const [isNotFound, setIsNotFound] =
        useState(false)

    const [
        errorMessage,
        setErrorMessage,
    ] = useState('')

    const [
        requestVersion,
        setRequestVersion,
    ] = useState(0)

    useEffect(() => {
        const controller =
            new AbortController()

        async function loadBook() {
            setIsLoading(true)
            setErrorMessage('')
            setIsNotFound(false)
            setBook(null)

            if (!bookId) {
                setIsNotFound(true)
                setIsLoading(false)

                return
            }

            try {
                const response =
                    await getBookById(
                        bookId,
                        controller.signal,
                    )

                if (!response) {
                    setIsNotFound(true)
                    return
                }

                setBook(response)
            } catch {
                if (
                    !controller.signal.aborted
                ) {
                    setErrorMessage(
                        'Kitap detayları yüklenemedi. ' +
                        'Lütfen tekrar deneyin.',
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

        void loadBook()

        return () => {
            controller.abort()
        }
    }, [bookId, requestVersion])

    if (isLoading) {
        return (
            <section className="book-detail-page">
                <div
                    className="catalog-status"
                    role="status"
                >
                    Kitap detayları yükleniyor...
                </div>
            </section>
        )
    }

    if (errorMessage) {
        return (
            <section className="book-detail-page">
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
                        Tekrar dene
                    </button>
                </div>
            </section>
        )
    }

    if (isNotFound || !book) {
        return (
            <section className="book-detail-page">
                <div className="catalog-status">
                    <strong>
                        Kitap bulunamadı
                    </strong>

                    <p>
                        Aradığınız kitap kaldırılmış
                        veya bağlantı hatalı olabilir.
                    </p>

                    <Link
                        className="button primary-button"
                        to="/catalog"
                    >
                        Kataloğa Dön
                    </Link>
                </div>
            </section>
        )
    }

    const primaryEdition =
        book.editions.find(
            (edition) =>
                edition.editionId ===
                book.primaryEditionId,
        ) ?? null

    const displayTitle =
        primaryEdition?.title || book.title

    const secondaryTitle =
        book.originalTitle &&
            book.originalTitle !== displayTitle
            ? book.originalTitle
            : book.title !== displayTitle
                ? book.title
                : null

    const displayCover =
        book.primaryCoverImageUrl ??
        primaryEdition?.coverImageUrl ??
        null

    const editions = [...book.editions]
        .sort((first, second) => {
            if (
                first.editionId ===
                book.primaryEditionId
            ) {
                return -1
            }

            if (
                second.editionId ===
                book.primaryEditionId
            ) {
                return 1
            }

            return first.languageCode
                .localeCompare(
                    second.languageCode,
                    'tr',
                )
        })

    return (
        <section className="book-detail-page">
            <Link
                className="book-detail-back-link"
                to="/catalog"
            >
                ← Kataloğa dön
            </Link>

            <div className="book-detail-hero">
                <div className="book-detail-cover">
                    <DetailCover
                        coverImageUrl={displayCover}
                        title={displayTitle}
                    />
                </div>

                <div className="book-detail-summary">
                    <p className="eyebrow">
                        Kitap detayları
                    </p>

                    <h1>{displayTitle}</h1>

                    {secondaryTitle && (
                        <p className="book-detail-original-title">
                            {secondaryTitle}
                        </p>
                    )}

                    <p className="book-detail-authors">
                        {book.authors.length > 0
                            ? book.authors
                                .map(
                                    (author) =>
                                        author.name,
                                )
                                .join(', ')
                            : 'Yazar bilgisi bulunmuyor'}
                    </p>

                    {book.categories.length > 0 && (
                        <div className="book-detail-categories">
                            {book.categories.map(
                                (category) => (
                                    <Link
                                        key={
                                            category.categoryId
                                        }
                                        to={
                                            '/catalog?' +
                                            new URLSearchParams({
                                                category:
                                                    category.slug,
                                                page: '1',
                                            }).toString()
                                        }
                                    >
                                        {category.name}
                                    </Link>
                                ),
                            )}
                        </div>
                    )}

                    <ReadingStatusPanel
                        bookId={book.bookId}
                    />

                    <BookProfileActionsPanel
                        bookId={book.bookId}
                    />


                    <dl className="book-detail-facts">
                        <dt>İlk yayın yılı</dt>
                        <dd>
                            {book.firstPublishedYear ??
                                'Bilinmiyor'}
                        </dd>

                        <dt>Orijinal dil</dt>
                        <dd>
                            {getLanguageName(
                                book.originalLanguageCode,
                            )}
                        </dd>

                        <dt>Kayıtlı baskı</dt>
                        <dd>
                            {book.editions.length}
                        </dd>
                    </dl>

                </div>
            </div>

            <BookRatingPanel bookId={book.bookId} />

            <section className="book-editions-section">
                <div className="book-editions-header">
                    <div>
                        <p className="eyebrow">
                            Yayın bilgileri
                        </p>

                        <h2>Baskılar</h2>
                    </div>

                    <span>
                        {editions.length} baskı
                    </span>
                </div>

                {editions.length === 0 ? (
                    <div className="catalog-status">
                        Bu kitap için kayıtlı baskı
                        bulunmuyor.
                    </div>
                ) : (
                    <div className="book-editions-grid">
                        {editions.map(
                            (
                                edition:
                                    BookEdition,
                            ) => {
                                const isPrimary =
                                    edition.editionId ===
                                    book.primaryEditionId

                                return (
                                    <article
                                        className={
                                            'book-edition-card' +
                                            (isPrimary
                                                ? ' primary-edition'
                                                : '')
                                        }
                                        key={
                                            edition.editionId
                                        }
                                    >
                                        <header>
                                            <div>
                                                <span className="book-edition-language">
                                                    {getLanguageName(
                                                        edition.languageCode,
                                                    )}
                                                </span>

                                                {isPrimary && (
                                                    <span className="primary-edition-badge">
                                                        Ana baskı
                                                    </span>
                                                )}
                                            </div>

                                            <h3>
                                                {
                                                    edition.title
                                                }
                                            </h3>
                                        </header>

                                        <dl>
                                            <dt>Format</dt>
                                            <dd>
                                                {getFormatName(
                                                    edition.format,
                                                )}
                                            </dd>

                                            <dt>Yayınevi</dt>
                                            <dd>
                                                {edition.publisher
                                                    ?.name ??
                                                    'Bilinmiyor'}
                                            </dd>

                                            <dt>Yayın yılı</dt>
                                            <dd>
                                                {edition.publicationYear ??
                                                    'Bilinmiyor'}
                                            </dd>

                                            <dt>Sayfa</dt>
                                            <dd>
                                                {edition.pageCount ??
                                                    'Bilinmiyor'}
                                            </dd>

                                            <dt>ISBN-13</dt>
                                            <dd>
                                                {edition.isbn13 ??
                                                    'Bulunmuyor'}
                                            </dd>

                                            <dt>ISBN-10</dt>
                                            <dd>
                                                {edition.isbn10 ??
                                                    'Bulunmuyor'}
                                            </dd>
                                        </dl>
                                    </article>
                                )
                            },
                        )}
                    </div>
                )}
            </section>
        </section>
    )
}

export default BookDetailPage
