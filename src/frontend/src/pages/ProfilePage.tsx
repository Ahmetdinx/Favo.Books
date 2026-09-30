import {
    useEffect,
    useState,
} from 'react'

import {
    Link,
    useNavigate,
} from 'react-router'

import {
    useAuthenticatedFetch,
} from '../auth/useAuthenticatedFetch'

import { useAuth } from '../auth/useAuth'

import ProfileFeaturedBooksSection from '../profile/ProfileFeaturedBooksSection'
import ReaderSettings from '../profile/ReaderSettings'
import ProfileAchievements from '../profile/ProfileAchievements'

import {
    getBookSummaries,
} from '../catalog/catalogApi'

import type {
    BookSummary,
} from '../catalog/catalogTypes'

import {
    getLibrary,
    getLibrarySummary,
    ReadingApiError,
} from '../reading/readingApi'

import type {
    LibraryEntry,
    LibraryEntryStatus,
    LibraryPageRequest,
    LibrarySummaryResponse,
} from '../reading/readingTypes'

type CurrentUserResponse = {
    userId: string
    email: string
}

type LibraryFilter =
    | 'all'
    | 'WantToRead'
    | 'Reading'
    | 'Read'

type ProfileLibraryItem = {
    entry: LibraryEntry
    book: BookSummary | null
}

type ProfileSummaryCard = {
    filter: LibraryFilter
    label: string
    description: string
    count: number
}

type ProfileBookCoverProps = {
    coverImageUrl: string | null
    title: string
}

const pageSize = 12

const filterOptions: ReadonlyArray<{
    value: LibraryFilter
    label: string
}> = [
        {
            value: 'all',
            label: 'Tümü',
        },
        {
            value: 'WantToRead',
            label: 'Okuma Listem',
        },
        {
            value: 'Reading',
            label: 'Okuyorum',
        },
        {
            value: 'Read',
            label: 'Okudum',
        },
    ]

const statusLabels: Record<
    LibraryEntryStatus,
    string
> = {
    WantToRead: 'Okuma Listem',
    Reading: 'Okuyorum',
    Read: 'Okudum',
    OnHold: 'Ara Verdim',
    Dropped: 'Yarım Bıraktım',
}

function ProfileBookCover({
    coverImageUrl,
    title,
}: ProfileBookCoverProps) {
    const [
        hasImageError,
        setHasImageError,
    ] = useState(false)

    if (!coverImageUrl || hasImageError) {
        const firstLetter =
            title
                .trim()
                .charAt(0)
                .toLocaleUpperCase('tr-TR') ||
            'K'

        return (
            <div className="profile-book-cover-placeholder">
                <span>{firstLetter}</span>
                <small>Kapak bulunamadı</small>
            </div>
        )
    }

    return (
        <img
            className="profile-book-cover-image"
            src={coverImageUrl}
            alt={`${title} kapak görseli`}
            loading="lazy"
            onError={() =>
                setHasImageError(true)
            }
        />
    )
}

function ProfilePage() {
    const {
        session,
        signOut,
    } = useAuth()

    const authenticatedFetch =
        useAuthenticatedFetch()

    const navigate = useNavigate()

    const [profile, setProfile] =
        useState<CurrentUserResponse | null>(
            null,
        )

    const [
        isProfileLoading,
        setIsProfileLoading,
    ] = useState(true)

    const [
        profileErrorMessage,
        setProfileErrorMessage,
    ] = useState('')

    const [
        profileRequestVersion,
        setProfileRequestVersion,
    ] = useState(0)

    const [
        selectedFilter,
        setSelectedFilter,
    ] = useState<LibraryFilter>('all')

    const [page, setPage] =
        useState(1)

    const [
        libraryItems,
        setLibraryItems,
    ] = useState<ProfileLibraryItem[]>([])

    const [
        totalCount,
        setTotalCount,
    ] = useState(0)

    const [
        isLibraryLoading,
        setIsLibraryLoading,
    ] = useState(false)

    const [
        libraryErrorMessage,
        setLibraryErrorMessage,
    ] = useState('')

    const [
        libraryRequestVersion,
        setLibraryRequestVersion,
    ] = useState(0)

    const [
        librarySummary,
        setLibrarySummary,
    ] = useState<LibrarySummaryResponse | null>(
        null,
    )

    const [
        isSummaryLoading,
        setIsSummaryLoading,
    ] = useState(false)

    const [
        summaryErrorMessage,
        setSummaryErrorMessage,
    ] = useState('')

    const [
        summaryRequestVersion,
        setSummaryRequestVersion,
    ] = useState(0)

    const userId = session?.userId
    const profileUserId = profile?.userId

    const totalPages =
        totalCount === 0
            ? 0
            : Math.ceil(
                totalCount / pageSize,
            )

    const summaryCards: ProfileSummaryCard[] =
        librarySummary
            ? [
                {
                    filter: 'all',
                    label: 'Toplam',
                    description:
                        'Kişisel kitaplığındaki ' +
                        'tüm kitaplar',
                    count:
                        librarySummary.totalCount,
                },
                {
                    filter: 'WantToRead',
                    label: 'Okuma Listem',
                    description:
                        'Sırada bekleyen ' +
                        'kitapların',
                    count:
                        librarySummary
                            .wantToReadCount,
                },
                {
                    filter: 'Reading',
                    label: 'Okuyorum',
                    description:
                        'Şu anda eşlik eden ' +
                        'kitapların',
                    count:
                        librarySummary
                            .readingCount,
                },
                {
                    filter: 'Read',
                    label: 'Okudum',
                    description:
                        'Tamamladığın kitapların',
                    count:
                        librarySummary.readCount,
                },
            ]
            : []

    useEffect(() => {
        if (!userId) {
            return
        }

        const controller =
            new AbortController()

        async function loadProfile() {
            setIsProfileLoading(true)
            setProfileErrorMessage('')
            setProfile(null)

            try {
                const response =
                    await authenticatedFetch(
                        '/api/identity/me',
                        {
                            signal:
                                controller.signal,
                        },
                    )

                if (response.status === 401) {
                    await signOut()

                    navigate('/login', {
                        replace: true,
                        state: {
                            from: '/profile',
                        },
                    })

                    return
                }

                if (!response.ok) {
                    setProfileErrorMessage(
                        'Profil bilgileri ' +
                        'yüklenemedi. Lütfen ' +
                        'tekrar deneyin.',
                    )

                    return
                }

                const currentUser =
                    (await response.json()) as
                    CurrentUserResponse

                setProfile(currentUser)
            } catch {
                if (
                    !controller.signal.aborted
                ) {
                    setProfileErrorMessage(
                        'Sunucuya ulaşılamadı. ' +
                        'API’nin çalıştığından ' +
                        'emin olun.',
                    )
                }
            } finally {
                if (
                    !controller.signal.aborted
                ) {
                    setIsProfileLoading(false)
                }
            }
        }

        void loadProfile()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        navigate,
        profileRequestVersion,
        signOut,
        userId,
    ])

    useEffect(() => {
        if (!userId || !profileUserId) {
            return
        }

        const controller =
            new AbortController()

        async function loadLibrarySummary() {
            setIsSummaryLoading(true)
            setSummaryErrorMessage('')
            setLibrarySummary(null)

            try {
                const response =
                    await getLibrarySummary(
                        authenticatedFetch,
                        controller.signal,
                    )

                setLibrarySummary(response)
            } catch (error) {
                if (
                    controller.signal.aborted
                ) {
                    return
                }

                if (
                    error instanceof
                    ReadingApiError &&
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

                setSummaryErrorMessage(
                    'Okuma istatistikleriniz ' +
                    'yüklenemedi.',
                )
            } finally {
                if (
                    !controller.signal.aborted
                ) {
                    setIsSummaryLoading(false)
                }
            }
        }

        void loadLibrarySummary()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        navigate,
        profileUserId,
        signOut,
        summaryRequestVersion,
        userId,
    ])

    useEffect(() => {
        if (!userId || !profileUserId) {
            return
        }

        const controller =
            new AbortController()

        async function loadLibrary() {
            setIsLibraryLoading(true)
            setLibraryErrorMessage('')
            setLibraryItems([])

            const request:
                LibraryPageRequest = {
                page,
                pageSize,
            }

            if (selectedFilter !== 'all') {
                request.status =
                    selectedFilter
            }

            try {
                const libraryResponse =
                    await getLibrary(
                        authenticatedFetch,
                        request,
                        controller.signal,
                    )

                const summariesResponse =
                    await getBookSummaries(
                        {
                            bookIds:
                                libraryResponse
                                    .items
                                    .map(
                                        (item) =>
                                            item.bookId,
                                    ),
                        },
                        controller.signal,
                    )

                const summariesByBookId =
                    new Map<
                        string,
                        BookSummary
                    >(
                        summariesResponse.items
                            .map(
                                (book) => [
                                    book.bookId,
                                    book,
                                ],
                            ),
                    )

                const items =
                    libraryResponse.items.map(
                        (entry) => ({
                            entry,
                            book:
                                summariesByBookId.get(
                                    entry.bookId,
                                ) ?? null,
                        }),
                    )

                setLibraryItems(items)

                setTotalCount(
                    libraryResponse.totalCount,
                )
            } catch (error) {
                if (
                    controller.signal.aborted
                ) {
                    return
                }

                if (
                    error instanceof
                    ReadingApiError &&
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

                setLibraryErrorMessage(
                    'Kitaplığınız yüklenemedi. ' +
                    'Lütfen tekrar deneyin.',
                )
            } finally {
                if (
                    !controller.signal.aborted
                ) {
                    setIsLibraryLoading(false)
                }
            }
        }

        void loadLibrary()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        libraryRequestVersion,
        navigate,
        page,
        profileUserId,
        selectedFilter,
        signOut,
        userId,
    ])

    useEffect(() => {
        if (
            totalPages > 0 &&
            page > totalPages
        ) {
            setPage(totalPages)
        }
    }, [
        page,
        totalPages,
    ])

    if (!session) {
        return null
    }

    if (isProfileLoading) {
        return (
            <section className="profile-page">
                <div
                    className="profile-page-state"
                    role="status"
                >
                    Profiliniz yükleniyor...
                </div>
            </section>
        )
    }

    if (profileErrorMessage) {
        return (
            <section className="profile-page">
                <div className="profile-page-state profile-page-error">
                    <p role="alert">
                        {profileErrorMessage}
                    </p>

                    <button
                        className={
                            'button primary-button'
                        }
                        type="button"
                        onClick={() =>
                            setProfileRequestVersion(
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

    if (!profile) {
        return null
    }

    return (
        <section className="profile-page">
            <header className="profile-page-header">
                <div>

                    <h1>Kitaplığım</h1>
                </div>
            </header>

            <ReaderSettings />
            <ProfileFeaturedBooksSection />
            <ProfileAchievements />
            <section
                className="profile-summary-section"
                aria-labelledby={
                    'reading-summary-heading'
                }
            >
                <h2 id="reading-summary-heading" className="profile-visually-hidden">Kitaplık özeti</h2>

                {isSummaryLoading && (
                    <div
                        className={
                            'profile-summary-message'
                        }
                        role="status"
                    >
                        İstatistikleriniz
                        yükleniyor...
                    </div>
                )}

                {!isSummaryLoading &&
                    summaryErrorMessage && (
                        <div className="profile-summary-message profile-summary-error">
                            <p role="alert">
                                {
                                    summaryErrorMessage
                                }
                            </p>

                            <button
                                className={
                                    'button ' +
                                    'secondary-button'
                                }
                                type="button"
                                onClick={() =>
                                    setSummaryRequestVersion(
                                        (
                                            current,
                                        ) =>
                                            current +
                                            1,
                                    )
                                }
                            >
                                Tekrar dene
                            </button>
                        </div>
                    )}

                {!isSummaryLoading &&
                    !summaryErrorMessage &&
                    librarySummary && (
                        <div className="profile-summary-grid">
                            {summaryCards.map(
                                (card) => (
                                    <button
                                        key={
                                            card.filter
                                        }
                                        className={
                                            'profile-summary-card' +
                                            (
                                                selectedFilter ===
                                                    card.filter
                                                    ? ' active'
                                                    : ''
                                            )
                                        }
                                        type="button"
                                        aria-pressed={
                                            selectedFilter ===
                                            card.filter
                                        }
                                        onClick={() => {
                                            setSelectedFilter(
                                                card.filter,
                                            )

                                            setPage(1)
                                        }}
                                    >
                                        <span>
                                            {
                                                card.label
                                            }
                                        </span>

                                        <strong>
                                            {
                                                card.count
                                            }
                                        </strong>
                                    </button>
                                ),
                            )}
                        </div>
                    )}
            </section>

            <section className="profile-library-section">
                <div className="profile-library-header">
                    <div>

                        <h2>Kitapların</h2>
                    </div>

                    <div className="profile-library-count">
                        <strong>
                            {totalCount}
                        </strong>

                        <span>
                            {selectedFilter ===
                                'all'
                                ? 'kitap'
                                : 'bu durumda kitap'}
                        </span>
                    </div>
                </div>

                <div
                    className="profile-filter-tabs"
                    aria-label={
                        'Kitaplık filtreleri'
                    }
                >
                    {filterOptions.map(
                        (option) => (
                            <button
                                key={
                                    option.value
                                }
                                className={
                                    'profile-filter-button' +
                                    (
                                        selectedFilter ===
                                            option.value
                                            ? ' active'
                                            : ''
                                    )
                                }
                                type="button"
                                aria-pressed={
                                    selectedFilter ===
                                    option.value
                                }
                                onClick={() => {
                                    setSelectedFilter(
                                        option.value,
                                    )

                                    setPage(1)
                                }}
                            >
                                {option.label}
                            </button>
                        ),
                    )}
                </div>

                {isLibraryLoading && (
                    <div
                        className={
                            'profile-library-state'
                        }
                        role="status"
                    >
                        Kitaplığınız yükleniyor...
                    </div>
                )}

                {!isLibraryLoading &&
                    libraryErrorMessage && (
                        <div className="profile-library-state profile-library-error">
                            <p role="alert">
                                {
                                    libraryErrorMessage
                                }
                            </p>

                            <button
                                className={
                                    'button ' +
                                    'secondary-button'
                                }
                                type="button"
                                onClick={() =>
                                    setLibraryRequestVersion(
                                        (
                                            current,
                                        ) =>
                                            current +
                                            1,
                                    )
                                }
                            >
                                Tekrar dene
                            </button>
                        </div>
                    )}

                {!isLibraryLoading &&
                    !libraryErrorMessage &&
                    libraryItems.length ===
                    0 && (
                        <div className="profile-library-state profile-library-empty">
                            <h3>
                                Bu bölüm henüz boş
                            </h3>

                            <Link
                                className={
                                    'button ' +
                                    'primary-button'
                                }
                                to="/catalog"
                            >
                                Kataloğu Keşfet
                            </Link>
                        </div>
                    )}

                {!isLibraryLoading &&
                    !libraryErrorMessage &&
                    libraryItems.length >
                    0 && (
                        <>
                            <div className="profile-library-grid">
                                {libraryItems.map(
                                    ({
                                        entry,
                                        book,
                                    }) => {
                                        const title =
                                            book?.title ??
                                            'Katalog kaydı bulunamadı'

                                        const authors =
                                            book?.authors
                                                .map(
                                                    (
                                                        author,
                                                    ) =>
                                                        author.name,
                                                )
                                                .join(
                                                    ', ',
                                                ) ??
                                            ''

                                        const statusClass =
                                            entry.status
                                                .toLowerCase()

                                        return (
                                            <article
                                                className={
                                                    'profile-book-card'
                                                }
                                                key={
                                                    entry.libraryEntryId
                                                }
                                            >
                                                <div className="profile-book-cover">
                                                    {book ? (
                                                        <Link
                                                            to={`/catalog/books/${book.bookId}`}
                                                            aria-label={`${book.title} detaylarını görüntüle`}
                                                        >
                                                            <ProfileBookCover
                                                                coverImageUrl={
                                                                    book.coverImageUrl
                                                                }
                                                                title={
                                                                    book.title
                                                                }
                                                            />
                                                        </Link>
                                                    ) : (
                                                        <ProfileBookCover
                                                            coverImageUrl={
                                                                null
                                                            }
                                                            title={
                                                                title
                                                            }
                                                        />
                                                    )}

                                                </div>
                                                    <span
                                                        className={
                                                            'profile-book-status ' +
                                                            `status-${statusClass}`
                                                        }
                                                    >
                                                        {
                                                            statusLabels[
                                                            entry
                                                                .status
                                                            ]
                                                        }
                                                    </span>

                                                <div className="profile-book-content">
                                                    {book
                                                        ?.firstPublishedYear && (
                                                            <p className="profile-book-year">
                                                                İlk
                                                                yayın:{' '}
                                                                {
                                                                    book.firstPublishedYear
                                                                }
                                                            </p>
                                                        )}

                                                    <h3>
                                                        {book ? (
                                                            <Link
                                                                to={`/catalog/books/${book.bookId}`}
                                                            >
                                                                {
                                                                    book.title
                                                                }
                                                            </Link>
                                                        ) : (
                                                            title
                                                        )}
                                                    </h3>

                                                    <p className="profile-book-authors">
                                                        {authors ||
                                                            'Yazar bilgisi yok'}
                                                    </p>

                                                    {book &&
                                                        book
                                                            .categories
                                                            .length >
                                                        0 && (
                                                            <div className="profile-book-categories">
                                                                {book.categories
                                                                    .slice(
                                                                        0,
                                                                        3,
                                                                    )
                                                                    .map(
                                                                        (
                                                                            category,
                                                                        ) => (
                                                                            <Link
                                                                                key={
                                                                                    category.categoryId
                                                                                }
                                                                                to={`/catalog?category=${encodeURIComponent(
                                                                                    category.slug,
                                                                                )}&page=1`}
                                                                            >
                                                                                {
                                                                                    category.name
                                                                                }
                                                                            </Link>
                                                                        ),
                                                                    )}
                                                            </div>
                                                        )}

                                                    {book ? (
                                                        <Link
                                                            className={
                                                                'profile-book-detail-link'
                                                            }
                                                            to={`/catalog/books/${book.bookId}`}
                                                        >
                                                            Kitabı
                                                            incele
                                                        </Link>
                                                    ) : (
                                                        <span className="profile-book-missing">
                                                            Kitap
                                                            katalogda
                                                            bulunamadı.
                                                        </span>
                                                    )}
                                                </div>
                                            </article>
                                        )
                                    },
                                )}
                            </div>

                            {totalPages > 1 && (
                                <nav
                                    className={
                                        'profile-pagination'
                                    }
                                    aria-label={
                                        'Kitaplık sayfaları'
                                    }
                                >
                                    <button
                                        className={
                                            'button ' +
                                            'secondary-button'
                                        }
                                        type="button"
                                        disabled={
                                            page <= 1
                                        }
                                        onClick={() =>
                                            setPage(
                                                (
                                                    current,
                                                ) =>
                                                    Math.max(
                                                        1,
                                                        current -
                                                        1,
                                                    ),
                                            )
                                        }
                                    >
                                        Önceki
                                    </button>

                                    <span>
                                        {page} /{' '}
                                        {totalPages}
                                    </span>

                                    <button
                                        className={
                                            'button ' +
                                            'secondary-button'
                                        }
                                        type="button"
                                        disabled={
                                            page >=
                                            totalPages
                                        }
                                        onClick={() =>
                                            setPage(
                                                (
                                                    current,
                                                ) =>
                                                    Math.min(
                                                        totalPages,
                                                        current +
                                                        1,
                                                    ),
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
        </section>
    )
}

export default ProfilePage
