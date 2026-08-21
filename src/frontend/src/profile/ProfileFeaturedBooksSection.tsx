import {
    useEffect,
    useState,
} from 'react'

import {
    Link,
    useNavigate,
} from 'react-router'

import { useAuth } from '../auth/useAuth'
import {
    useAuthenticatedFetch,
} from '../auth/useAuthenticatedFetch'
import {
    getBookSummaries,
} from '../catalog/catalogApi'
import type {
    BookSummary,
} from '../catalog/catalogTypes'

import {
    getBedsideBook,
    getInfluentialBooks,
    ProfileApiError,
} from './profileApi'
import type {
    BedsideBookSelection,
    InfluentialBookSelection,
} from './profileTypes'

import './ProfileFeaturedBooksSection.css'

type FeaturedBookCoverProps = {
    coverImageUrl: string | null
    title: string
}

const influentialPositions = [
    1,
    2,
    3,
    4,
    5,
] as const

function FeaturedBookCover({
    coverImageUrl,
    title,
}: FeaturedBookCoverProps) {
    const [hasImageError, setHasImageError] =
        useState(false)

    if (!coverImageUrl || hasImageError) {
        const firstLetter =
            title
                .trim()
                .charAt(0)
                .toLocaleUpperCase('tr-TR') ||
            'K'

        return (
            <div className="profile-featured-cover-placeholder">
                <span>{firstLetter}</span>
                <small>Kapak bulunamadı</small>
            </div>
        )
    }

    return (
        <img
            className="profile-featured-cover-image"
            src={coverImageUrl}
            alt={`${title} kapak görseli`}
            loading="lazy"
            onError={() =>
                setHasImageError(true)
            }
        />
    )
}

function getAuthors(
    book: BookSummary | null,
) {
    const authors =
        book?.authors
            .map((author) => author.name)
            .join(', ') ?? ''

    return authors || 'Yazar bilgisi yok'
}

function ProfileFeaturedBooksSection() {
    const {
        session,
        signOut,
    } = useAuth()

    const authenticatedFetch =
        useAuthenticatedFetch()

    const navigate = useNavigate()

    const [
        bedsideSelection,
        setBedsideSelection,
    ] = useState<
        BedsideBookSelection | null
    >(null)

    const [
        influentialSelections,
        setInfluentialSelections,
    ] = useState<
        InfluentialBookSelection[]
    >([])

    const [
        bookSummaries,
        setBookSummaries,
    ] = useState<BookSummary[]>([])

    const [isLoading, setIsLoading] =
        useState(true)

    const [errorMessage, setErrorMessage] =
        useState('')

    const [requestVersion, setRequestVersion] =
        useState(0)

    const userId = session?.userId

    useEffect(() => {
        if (!userId) {
            return
        }

        const controller =
            new AbortController()

        async function loadFeaturedBooks() {
            setIsLoading(true)
            setErrorMessage('')

            try {
                const [
                    bedsideResponse,
                    influentialResponse,
                ] = await Promise.all([
                    getBedsideBook(
                        authenticatedFetch,
                        controller.signal,
                    ),
                    getInfluentialBooks(
                        authenticatedFetch,
                        controller.signal,
                    ),
                ])

                const bookIds = Array.from(
                    new Set([
                        ...(bedsideResponse
                            ? [
                                bedsideResponse
                                    .bookId,
                            ]
                            : []),
                        ...influentialResponse
                            .items
                            .map(
                                (item) =>
                                    item.bookId,
                            ),
                    ]),
                )

                const summariesResponse =
                    bookIds.length > 0
                        ? await getBookSummaries(
                            { bookIds },
                            controller.signal,
                        )
                        : { items: [] }

                setBedsideSelection(
                    bedsideResponse,
                )

                setInfluentialSelections(
                    influentialResponse.items,
                )

                setBookSummaries(
                    summariesResponse.items,
                )
            } catch (error) {
                if (controller.signal.aborted) {
                    return
                }

                if (
                    error instanceof
                    ProfileApiError &&
                    error.status === 401
                ) {
                    await signOut()

                    navigate('/login', {
                        replace: true,
                        state: {
                            from: '/profile',
                        },
                    })

                    return
                }

                setErrorMessage(
                    'Öne çıkan kitaplarınız ' +
                    'yüklenemedi.',
                )
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false)
                }
            }
        }

        void loadFeaturedBooks()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        navigate,
        requestVersion,
        signOut,
        userId,
    ])

    const booksById = new Map<
        string,
        BookSummary
    >(
        bookSummaries.map((book) => [
            book.bookId,
            book,
        ]),
    )

    const bedsideBook =
        bedsideSelection
            ? booksById.get(
                bedsideSelection.bookId,
            ) ?? null
            : null

    const selectionCount =
        (bedsideSelection ? 1 : 0) +
        influentialSelections.length

    if (isLoading) {
        return (
            <section className="profile-featured-section">
                <div
                    className="profile-featured-state"
                    role="status"
                >
                    Okur vitrininiz yükleniyor...
                </div>
            </section>
        )
    }

    if (errorMessage) {
        return (
            <section className="profile-featured-section">
                <div className="profile-featured-state profile-featured-error">
                    <p role="alert">
                        {errorMessage}
                    </p>

                    <button
                        className={
                            'button ' +
                            'secondary-button'
                        }
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
            </section>
        )
    }

    return (
        <section
            className="profile-featured-section"
            aria-labelledby={
                'featured-books-heading'
            }
        >
            <div className="profile-featured-header">
                <div>
                    <p className="eyebrow">
                        Okur kimliğin
                    </p>

                    <h2 id="featured-books-heading">
                        Kitaplarda bıraktığın iz
                    </h2>

                    <p>
                        Sana eşlik eden ve okurluk
                        yolculuğunu değiştiren
                        kitaplar.
                    </p>
                </div>

                <div className="profile-featured-count">
                    <strong>{selectionCount}</strong>
                    <span>6 özel seçimden</span>
                </div>
            </div>

            {selectionCount === 0 && (
                <div className="profile-featured-empty-intro">
                    <div>
                        <strong>
                            Okur vitrinin henüz boş
                        </strong>

                        <span>
                            Kitap detaylarından bir
                            başucu kitabı ve seni en
                            çok etkileyen beş kitabı
                            seçebilirsin.
                        </span>
                    </div>

                    <Link
                        className={
                            'button primary-button'
                        }
                        to="/catalog"
                    >
                        Kitap Seç
                    </Link>
                </div>
            )}

            <div className="profile-featured-grid">
                <article className="profile-bedside-showcase">
                    <div className="profile-bedside-cover-shell">
                        {bedsideBook ? (
                            <Link
                                to={
                                    '/catalog/books/' +
                                    bedsideBook.bookId
                                }
                                aria-label={
                                    `${bedsideBook.title} ` +
                                    'detaylarını görüntüle'
                                }
                            >
                                <FeaturedBookCover
                                    coverImageUrl={
                                        bedsideBook
                                            .coverImageUrl
                                    }
                                    title={
                                        bedsideBook.title
                                    }
                                />
                            </Link>
                        ) : (
                            <FeaturedBookCover
                                coverImageUrl={null}
                                title="Başucu kitabım"
                            />
                        )}
                    </div>

                    <div className="profile-bedside-content">
                        <p className="profile-featured-kicker">
                            Başucu kitabım
                        </p>

                        {bedsideSelection ? (
                            <>
                                <h3>
                                    {bedsideBook ? (
                                        <Link
                                            to={
                                                '/catalog/books/' +
                                                bedsideBook.bookId
                                            }
                                        >
                                            {
                                                bedsideBook
                                                    .title
                                            }
                                        </Link>
                                    ) : (
                                        'Katalog kaydı ' +
                                        'bulunamadı'
                                    )}
                                </h3>

                                <p className="profile-featured-authors">
                                    {getAuthors(
                                        bedsideBook,
                                    )}
                                </p>

                                {bedsideSelection
                                    .personalNote ? (
                                    <blockquote>
                                        {
                                            bedsideSelection
                                                .personalNote
                                        }
                                    </blockquote>
                                ) : (
                                    <p className="profile-featured-note-empty">
                                        Bu kitap için
                                        henüz kişisel
                                        bir not
                                        eklemedin.
                                    </p>
                                )}
                            </>
                        ) : (
                            <div className="profile-bedside-empty">
                                <h3>
                                    Sana hep eşlik eden
                                    kitap hangisi?
                                </h3>

                                <p>
                                    Kitap detayından tek
                                    bir kitabı başucu
                                    kitabın olarak
                                    seçebilirsin.
                                </p>
                            </div>
                        )}
                    </div>
                </article>

                <section className="profile-influential-showcase">
                    <header>
                        <div>
                            <p className="profile-featured-kicker">
                                Kişisel kanon
                            </p>

                            <h3>
                                Beni en çok etkileyenler
                            </h3>
                        </div>

                        <span>
                            {
                                influentialSelections
                                    .length
                            }
                            /5
                        </span>
                    </header>

                    <ol className="profile-influential-list">
                        {influentialPositions.map(
                            (position) => {
                                const selection =
                                    influentialSelections
                                        .find(
                                            (item) =>
                                                item.position ===
                                                position,
                                        ) ?? null

                                const book = selection
                                    ? booksById.get(
                                        selection.bookId,
                                    ) ?? null
                                    : null

                                return (
                                    <li key={position}>
                                        <span className="profile-influential-rank">
                                            {position
                                                .toString()
                                                .padStart(
                                                    2,
                                                    '0',
                                                )}
                                        </span>

                                        {selection ? (
                                            <>
                                                <div className="profile-influential-cover">
                                                    {book ? (
                                                        <Link
                                                            to={
                                                                '/catalog/books/' +
                                                                book.bookId
                                                            }
                                                        >
                                                            <FeaturedBookCover
                                                                coverImageUrl={
                                                                    book.coverImageUrl
                                                                }
                                                                title={
                                                                    book.title
                                                                }
                                                            />
                                                        </Link>
                                                    ) : (
                                                        <FeaturedBookCover
                                                            coverImageUrl={
                                                                null
                                                            }
                                                            title="Kitap"
                                                        />
                                                    )}
                                                </div>

                                                <div className="profile-influential-content">
                                                    <h4>
                                                        {book ? (
                                                            <Link
                                                                to={
                                                                    '/catalog/books/' +
                                                                    book.bookId
                                                                }
                                                            >
                                                                {
                                                                    book.title
                                                                }
                                                            </Link>
                                                        ) : (
                                                            'Katalog kaydı bulunamadı'
                                                        )}
                                                    </h4>

                                                    <p className="profile-featured-authors">
                                                        {getAuthors(
                                                            book,
                                                        )}
                                                    </p>

                                                    {selection
                                                        .personalReflection && (
                                                            <p className="profile-influential-reflection">
                                                                {
                                                                    selection
                                                                        .personalReflection
                                                                }
                                                            </p>
                                                        )}
                                                </div>
                                            </>
                                        ) : (
                                            <div className="profile-influential-empty">
                                                <strong>
                                                    Bu sıra
                                                    henüz boş
                                                </strong>

                                                <span>
                                                    Seni
                                                    değiştiren
                                                    bir kitaba
                                                    ayır.
                                                </span>
                                            </div>
                                        )}
                                    </li>
                                )
                            },
                        )}
                    </ol>
                </section>
            </div>

            <div className="profile-featured-footer">
                <span>
                    Seçimlerini kitap detaylarından
                    yönetebilirsin.
                </span>

                <Link to="/catalog">
                    Kataloğa git
                </Link>
            </div>
        </section>
    )
}

export default ProfileFeaturedBooksSection