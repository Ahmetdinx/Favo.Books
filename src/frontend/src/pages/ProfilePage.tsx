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
import TasteCard from '../profile/TasteCard'
import ReaderSettings from '../profile/ReaderSettings'
import { ReaderConnections } from '../profile/ReaderSocial'
import ProfileAchievements from '../profile/ProfileAchievements'
import BookObject from '../components/BookObject'
import { cleanBookTitle } from '../catalog/bookTitle'

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

    // My own scores, shown as a small seal on each rated book like on a public profile.
    const [scores, setScores] = useState<Map<string, number>>(new Map())
    const ratedBookIds = libraryItems.flatMap(({ book }) => book ? [book.bookId] : []).join(',')
    useEffect(() => {
        const controller = new AbortController()
        if (!ratedBookIds) { setScores(new Map()); return }
        void authenticatedFetch('/api/readers/me/ratings?bookIds=' + ratedBookIds, { signal: controller.signal, cache: 'no-store' })
            .then(async response => {
                if (!response.ok) throw new Error('Ratings unavailable')
                const data = await response.json() as { items: { bookId: string; overallScore: number | null }[] }
                if (!controller.signal.aborted) setScores(new Map(data.items.flatMap(item =>
                    item.overallScore == null ? [] : [[item.bookId, item.overallScore] as const])))
            })
            .catch(() => { if (!controller.signal.aborted) setScores(new Map()) }) // The seals are optional.
        return () => controller.abort()
    }, [authenticatedFetch, ratedBookIds])

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

    const tabCounts: Record<LibraryFilter, number | undefined> = {
        all: librarySummary?.totalCount,
        WantToRead: librarySummary?.wantToReadCount,
        Reading: librarySummary?.readingCount,
        Read: librarySummary?.readCount,
    }

    return (
        <section className="profile-page">
            <header className="library-room-header">
                <h1>Kitaplığım</h1>
                {librarySummary && <p>{librarySummary.totalCount} kitap · {librarySummary.readCount} tanesi okundu</p>}
            </header>

            <TasteCard own />

            <ProfileFeaturedBooksSection />

            <section className="bookcase-section" aria-labelledby="bookcase-heading">
                <h2 id="bookcase-heading" className="profile-visually-hidden">Raflarım</h2>

                <div className="bookcase-tabs" role="group" aria-label="Kitaplık rafları">
                    {filterOptions.map(option => (
                        <button key={option.value} type="button"
                            aria-pressed={selectedFilter === option.value}
                            onClick={() => { setSelectedFilter(option.value); setPage(1) }}>
                            {option.label}
                            <strong>{tabCounts[option.value] ?? (isSummaryLoading ? '…' : '')}</strong>
                        </button>
                    ))}
                </div>

                {isLibraryLoading && <div className="bookcase-state" role="status">Raflar diziliyor…</div>}

                {!isLibraryLoading && libraryErrorMessage && (
                    <div className="bookcase-state">
                        <p role="alert">{libraryErrorMessage}</p>
                        <button className="button secondary-button" type="button"
                            onClick={() => setLibraryRequestVersion(current => current + 1)}>
                            Tekrar dene
                        </button>
                    </div>
                )}

                {!isLibraryLoading && !libraryErrorMessage && libraryItems.length === 0 && (
                    <div className="bookcase-state">
                        <h3>Bu raf henüz boş</h3>
                        <Link className="button primary-button" to="/catalog">Kataloğu keşfet</Link>
                    </div>
                )}

                {!isLibraryLoading && !libraryErrorMessage && libraryItems.length > 0 && (
                    <div className="bookcase">
                        {libraryItems.map(({ entry, book }) => {
                            const title = book ? cleanBookTitle(book.title) : 'Katalog kaydı bulunamadı'
                            const authors = book?.authors.map(author => author.name).join(', ') ?? ''
                            const volume = <BookObject title={title} author={authors} coverImageUrl={book?.coverImageUrl} />
                            return (
                                <article className="bookcase-slot" key={entry.libraryEntryId}>
                                    <div className="bookcase-book">
                                        {book
                                            ? <Link to={`/catalog/books/${book.bookId}`} aria-label={`${title} detaylarını görüntüle`}>{volume}</Link>
                                            : volume}
                                        <span className={`bookcase-ribbon status-${entry.status.toLowerCase()}`}>{statusLabels[entry.status]}</span>
                                        {book && scores.has(book.bookId) && (
                                            <span className="bookcase-score" aria-label={`Puanın ${scores.get(book.bookId)!.toFixed(1)} / 10`}
                                                title={`Puanın: ${scores.get(book.bookId)!.toLocaleString('tr-TR', { maximumFractionDigits: 2 })} / 10`}>
                                                {scores.get(book.bookId)!.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}
                                            </span>
                                        )}
                                    </div>
                                    <div className="bookcase-label">
                                        <h3 title={title}>{book ? <Link to={`/catalog/books/${book.bookId}`}>{title}</Link> : title}</h3>
                                        <p title={authors}>{authors || 'Yazar bilgisi yok'}</p>
                                    </div>
                                </article>
                            )
                        })}
                    </div>
                )}

                {summaryErrorMessage && !isSummaryLoading && (
                    <p className="bookcase-note" role="alert">
                        {summaryErrorMessage}{' '}
                        <button type="button" onClick={() => setSummaryRequestVersion(current => current + 1)}>Tekrar dene</button>
                    </p>
                )}

                {totalPages > 1 && !isLibraryLoading && (
                    <nav className="profile-pagination" aria-label="Kitaplık sayfaları">
                        <button className="button secondary-button" type="button" disabled={page <= 1}
                            onClick={() => setPage(current => Math.max(1, current - 1))}>Önceki</button>
                        <span>{page} / {totalPages}</span>
                        <button className="button secondary-button" type="button" disabled={page >= totalPages}
                            onClick={() => setPage(current => Math.min(totalPages, current + 1))}>Sonraki</button>
                    </nav>
                )}
            </section>

            <ProfileAchievements />
            <ReaderConnections />
            <details className="library-settings">
                <summary>Profil ayarları</summary>
                <ReaderSettings />
            </details>
        </section>
    )
}

export default ProfilePage
