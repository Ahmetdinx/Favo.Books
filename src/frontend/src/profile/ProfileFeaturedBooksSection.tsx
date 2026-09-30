import {
    useEffect,
    useRef,
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
    const shelfRef = useRef<HTMLOListElement>(null)

    function scrollShelf(direction: number) {
        const shelf = shelfRef.current
        if (!shelf) return
        const card = shelf.querySelector('li')
        shelf.scrollBy({ left: direction * ((card?.getBoundingClientRect().width ?? 260) + 16) })
    }

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
                        Tekrar dene
                    </button>
                </div>
            </section>
        )
    }

    return (
        <section className="profile-featured-section" aria-label="Kişisel kitap rafım">
            <article className="profile-bedside-showcase" aria-labelledby="bedside-heading">
                <div className="profile-bedside-cover-shell">
                    {bedsideBook ? (
                        <Link to={'/catalog/books/' + bedsideBook.bookId}>
                            <FeaturedBookCover key={bedsideBook.coverImageUrl} coverImageUrl={bedsideBook.coverImageUrl} title={bedsideBook.title} />
                        </Link>
                    ) : (
                        <div className="profile-bedside-empty-cover" aria-hidden="true">
                            <span>K</span><small>Başucu<br />kitabım</small>
                        </div>
                    )}
                </div>
                <div className="profile-bedside-content">
                    <h2 id="bedside-heading">Başucu kitabım</h2>
                    {bedsideSelection ? (
                        <>
                            <h3>{bedsideBook ? (
                                <Link to={'/catalog/books/' + bedsideBook.bookId}>{bedsideBook.title}</Link>
                            ) : 'Katalog kaydı bulunamadı'}</h3>
                            <p className="profile-featured-authors">{getAuthors(bedsideBook)}</p>
                            {bedsideSelection.personalNote && <details className="profile-book-note"><summary>Notum</summary><blockquote>{bedsideSelection.personalNote}</blockquote></details>}
                        </>
                    ) : null}
                    <Link className="profile-shelf-link" to={bedsideBook ? '/catalog/books/' + bedsideBook.bookId : '/catalog'}>
                        {bedsideBook ? 'Kitaba git' : 'Kitap seç'} <span aria-hidden="true">↗</span>
                    </Link>
                </div>
            </article>

            <section className="profile-influential-showcase" aria-labelledby="influential-heading">
                <header>
                    <div>
                        <h2 id="influential-heading">İz bırakanlar</h2>
                    </div>
                    <div className="profile-shelf-controls">
                        <span aria-label={influentialSelections.length + ' kitap seçildi, en fazla 5'}>{influentialSelections.length} / 5</span>
                        {influentialSelections.length > 0 && (
                            <>
                                <button type="button" aria-label="Önceki kitaplara kaydır" aria-controls="influential-shelf" onClick={() => scrollShelf(-1)}>←</button>
                                <button type="button" aria-label="Sonraki kitaplara kaydır" aria-controls="influential-shelf" onClick={() => scrollShelf(1)}>→</button>
                            </>
                        )}
                    </div>
                </header>

                {influentialSelections.length === 0 ? (
                    <div className="profile-shelf-empty">
                        <Link className="button secondary-button" to="/catalog">Roman seç</Link>
                    </div>
                ) : (
                    <ol id="influential-shelf" className="profile-influential-list" ref={shelfRef} tabIndex={0} aria-label="Beni en çok etkileyen beş kitap; yatay kaydırılabilir raf">
                        {influentialPositions.map(position => {
                            const selection = influentialSelections.find(item => item.position === position)
                            const book = selection ? booksById.get(selection.bookId) ?? null : null
                            return (
                                <li key={position} className={selection ? 'profile-shelf-card' : 'profile-shelf-card profile-shelf-card-empty'}>
                                    <span className="profile-influential-rank" aria-label={position + '. sıra'}>{String(position).padStart(2, '0')}</span>
                                    {selection ? (
                                        <>
                                            <div className="profile-influential-cover">
                                                {book ? (
                                                    <Link to={'/catalog/books/' + book.bookId}>
                                                        <FeaturedBookCover key={book.coverImageUrl} coverImageUrl={book.coverImageUrl} title={book.title} />
                                                    </Link>
                                                ) : <FeaturedBookCover coverImageUrl={null} title="Kitap" />}
                                            </div>
                                            <div className="profile-influential-content">
                                                <h3>{book ? <Link to={'/catalog/books/' + book.bookId}>{book.title}</Link> : 'Katalog kaydı bulunamadı'}</h3>
                                                <p className="profile-featured-authors">{getAuthors(book)}</p>
                                                {selection.personalReflection && <details className="profile-book-note"><summary>Notum</summary><blockquote className="profile-influential-reflection">{selection.personalReflection}</blockquote></details>}
                                            </div>
                                        </>
                                    ) : (
                                        <Link className="profile-influential-empty" to="/catalog">
                                            <span className="profile-shelf-add" aria-hidden="true">+</span>
                                            <strong>Kitap ekle</strong>

                                        </Link>
                                    )}
                                </li>
                            )
                        })}
                    </ol>
                )}
            </section>
        </section>
    )
}

export default ProfileFeaturedBooksSection
