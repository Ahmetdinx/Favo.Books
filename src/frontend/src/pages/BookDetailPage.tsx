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
import BookObject from '../components/BookObject'
import { cleanBookTitle } from '../catalog/bookTitle'
import './BookDetail.css'

import type {
    BookDetailsResponse,
    BookEdition,
} from '../catalog/catalogTypes'

const languageNames: Record<string, string> = {
    tur: 'Türkçe', tr: 'Türkçe', eng: 'İngilizce', en: 'İngilizce',
    fre: 'Fransızca', fra: 'Fransızca', fr: 'Fransızca',
    ger: 'Almanca', deu: 'Almanca', de: 'Almanca',
    rus: 'Rusça', ru: 'Rusça', spa: 'İspanyolca', es: 'İspanyolca',
    ita: 'İtalyanca', it: 'İtalyanca', jpn: 'Japonca', ja: 'Japonca',
    por: 'Portekizce', pt: 'Portekizce', chi: 'Çince', zho: 'Çince',
    ara: 'Arapça', per: 'Farsça', fas: 'Farsça', gre: 'Yunanca', ell: 'Yunanca',
    cze: 'Çekçe', ces: 'Çekçe', pol: 'Lehçe', swe: 'İsveççe', nor: 'Norveççe',
    dan: 'Danca', dut: 'Felemenkçe', nld: 'Felemenkçe', hun: 'Macarca',
}

/** Turkish language name, or null when the language is unknown. */
function getLanguageName(languageCode: string) {
    const code = languageCode.toLowerCase()
    if (!code || code === 'und' || code === 'mul') return null
    if (languageNames[code]) return languageNames[code]
    try {
        return new Intl.DisplayNames(['tr'], { type: 'language' }).of(code) ?? null
    } catch {
        return null
    }
}

const formatNames: Record<string, string> = {
    hardcover: 'Ciltli',
    paperback: 'Karton kapak',
    ebook: 'E-kitap',
    audiobook: 'Sesli kitap',
}

function editionFacts(edition: BookEdition) {
    return [
        ['Yayınevi', edition.publisher?.name],
        ['Yıl', edition.publicationYear?.toString()],
        ['Sayfa', edition.pageCount?.toString()],
        ['Format', formatNames[edition.format.toLowerCase()]],
        ['ISBN', edition.isbn13 ?? edition.isbn10],
    ].filter((fact): fact is [string, string] => !!fact[1])
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

    const authorNames = book.authors.map((author) => author.name).join(', ')
    const originalLanguage = getLanguageName(book.originalLanguageCode)
    const facts = [
        book.firstPublishedYear?.toString(),
        originalLanguage && `${originalLanguage} aslından`,
        editions.length > 1 ? `${editions.length} baskı` : null,
    ].filter(Boolean)

    return (
        <section className="book-detail-page">
            <Link className="book-detail-back-link" to="/catalog">
                ← Katalog
            </Link>

            <div className="book-spread">
                <div className="book-spread-cover">
                    <BookObject
                        title={displayTitle}
                        author={authorNames}
                        coverImageUrl={displayCover}
                        size="lg"
                        priority
                    />
                </div>

                <div className="book-spread-copy">
                    <h1>{cleanBookTitle(displayTitle)}</h1>

                    {secondaryTitle && (
                        <p className="book-spread-original">
                            {secondaryTitle}
                        </p>
                    )}

                    {authorNames && (
                        <p className="book-spread-author">{authorNames}</p>
                    )}

                    {(facts.length > 0 || book.categories.length > 0) && (
                        <p className="book-spread-facts">
                            {facts.join(' · ')}
                            {book.categories.map((category) => (
                                <Link
                                    key={category.categoryId}
                                    to={'/catalog?' + new URLSearchParams({ category: category.slug, page: '1' }).toString()}
                                >
                                    {category.name}
                                </Link>
                            ))}
                        </p>
                    )}

                    {book.description && (
                        <p className="book-spread-summary">{book.description}</p>
                    )}

                    <ReadingStatusPanel bookId={book.bookId} />
                    <BookProfileActionsPanel bookId={book.bookId} />
                </div>
            </div>

            <BookRatingPanel bookId={book.bookId} />

            {editions.length > 0 && (
                <section className="book-editions-section" aria-labelledby="book-editions-heading">
                    <h2 id="book-editions-heading">
                        {editions.length > 1 ? 'Baskılar' : 'Baskı'}
                    </h2>

                    <div className="edition-leaves">
                        {editions.map((edition) => (
                            <article className="edition-leaf" key={edition.editionId}>
                                <h3>{cleanBookTitle(edition.title)}</h3>
                                {getLanguageName(edition.languageCode) && edition.languageCode.toLowerCase() !== 'tur' && (
                                    <p className="edition-leaf-language">{getLanguageName(edition.languageCode)}</p>
                                )}
                                <dl>
                                    {editionFacts(edition).map(([label, value]) => (
                                        <div key={label}>
                                            <dt>{label}</dt>
                                            <dd>{value}</dd>
                                        </div>
                                    ))}
                                </dl>
                            </article>
                        ))}
                    </div>
                </section>
            )}
        </section>
    )
}

export default BookDetailPage
