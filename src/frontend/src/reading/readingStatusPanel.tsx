import {
    useEffect,
    useState,
} from 'react'

import { Link } from 'react-router'

import { useAuth } from '../auth/useAuth'
import {
    useAuthenticatedFetch,
} from '../auth/useAuthenticatedFetch'

import {
    addBookToLibrary,
    getLibraryEntryByBook,
    ReadingApiError,
    removeBookFromLibrary,
    updateLibraryStatus,
} from './readingApi'

import type {
    LibraryEntry,
    LibraryEntryStatus,
} from './readingTypes'

type ReadingStatusPanelProps = {
    bookId: string
}

type StatusOption = {
    status: LibraryEntryStatus
    label: string
}

const statusOptions: StatusOption[] = [
    {
        status: 'WantToRead',
        label: 'Okuma Listem',
    },
    {
        status: 'Reading',
        label: 'Okuyorum',
    },
    {
        status: 'Read',
        label: 'Okudum',
    },
]

const statusLabels:
    Record<LibraryEntryStatus, string> = {
    WantToRead: 'Okuma Listemde',
    Reading: 'Şu anda okuyorum',
    Read: 'Okudum',
    OnHold: 'Ara verdim',
    Dropped: 'Yarım bıraktım',
}

function getErrorMessage(error: unknown) {
    if (error instanceof ReadingApiError) {
        if (error.status === 401) {
            return (
                'Oturumunuz sona erdi. ' +
                'Lütfen tekrar giriş yapın.'
            )
        }

        if (error.status === 409) {
            return (
                'Bu kitap zaten kütüphanenizde. ' +
                'Durum bilgisi yenileniyor.'
            )
        }

        if (error.status === 404) {
            return (
                'Kütüphane kaydı bulunamadı. ' +
                'Durum bilgisi yenilendi.'
            )
        }
    }

    return (
        'İşlem tamamlanamadı. ' +
        'Lütfen tekrar deneyin.'
    )
}

function ReadingStatusPanel({
    bookId,
}: ReadingStatusPanelProps) {
    const {
        session,
        isInitializing,
    } = useAuth()

    const authenticatedFetch =
        useAuthenticatedFetch()

    const userId = session?.userId ?? null

    const [
        libraryEntry,
        setLibraryEntry,
    ] = useState<LibraryEntry | null>(null)

    const [isLoading, setIsLoading] =
        useState(true)

    const [isSaving, setIsSaving] =
        useState(false)

    const [
        loadErrorMessage,
        setLoadErrorMessage,
    ] = useState('')

    const [
        actionErrorMessage,
        setActionErrorMessage,
    ] = useState('')

    const [
        successMessage,
        setSuccessMessage,
    ] = useState('')

    const [
        requestVersion,
        setRequestVersion,
    ] = useState(0)

    useEffect(() => {
        if (isInitializing) {
            setIsLoading(true)
            return
        }

        if (!userId) {
            setLibraryEntry(null)
            setLoadErrorMessage('')
            setIsLoading(false)
            return
        }

        const controller =
            new AbortController()

        async function loadLibraryEntry() {
            setIsLoading(true)
            setLoadErrorMessage('')
            setActionErrorMessage('')
            setSuccessMessage('')

            try {
                const entry =
                    await getLibraryEntryByBook(
                        authenticatedFetch,
                        bookId,
                        controller.signal,
                    )

                setLibraryEntry(entry)
            } catch {
                if (!controller.signal.aborted) {
                    setLoadErrorMessage(
                        'Okuma durumunuz yüklenemedi.',
                    )
                }
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false)
                }
            }
        }

        void loadLibraryEntry()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        bookId,
        isInitializing,
        requestVersion,
        userId,
    ])

    async function handleStatusChange(
        status: LibraryEntryStatus,
    ) {
        if (
            !userId ||
            isSaving ||
            libraryEntry?.status === status
        ) {
            return
        }

        setIsSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            if (!libraryEntry) {
                const response =
                    await addBookToLibrary(
                        authenticatedFetch,
                        {
                            bookId,
                            status,
                        },
                    )

                const now =
                    new Date().toISOString()

                setLibraryEntry({
                    libraryEntryId:
                        response.libraryEntryId,
                    bookId,
                    status,
                    createdAtUtc: now,
                    updatedAtUtc: now,
                })

                setSuccessMessage(
                    'Kitap kütüphanenize eklendi.',
                )

                return
            }

            const response =
                await updateLibraryStatus(
                    authenticatedFetch,
                    libraryEntry.libraryEntryId,
                    status,
                )

            setLibraryEntry((current) => {
                if (!current) {
                    return current
                }

                return {
                    ...current,
                    status: response.status,
                    updatedAtUtc:
                        response.updatedAtUtc,
                }
            })

            setSuccessMessage(
                'Okuma durumunuz güncellendi.',
            )
        } catch (error) {
            if (
                error instanceof ReadingApiError &&
                (
                    error.status === 404 ||
                    error.status === 409
                )
            ) {
                setRequestVersion(
                    (current) => current + 1,
                )
            }

            setActionErrorMessage(
                getErrorMessage(error),
            )
        } finally {
            setIsSaving(false)
        }
    }

    async function handleRemove() {
        if (!libraryEntry || isSaving) {
            return
        }

        setIsSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            await removeBookFromLibrary(
                authenticatedFetch,
                libraryEntry.libraryEntryId,
            )

            setLibraryEntry(null)

            setSuccessMessage(
                'Kitap kütüphanenizden çıkarıldı.',
            )
        } catch (error) {
            if (
                error instanceof ReadingApiError &&
                error.status === 404
            ) {
                setLibraryEntry(null)
            }

            setActionErrorMessage(
                getErrorMessage(error),
            )
        } finally {
            setIsSaving(false)
        }
    }

    if (isInitializing || isLoading) {
        return (
            <section
                className="reading-status-panel"
                aria-label="Okuma durumu"
            >
                <p className="reading-status-loading">
                    Okuma durumunuz yükleniyor...
                </p>
            </section>
        )
    }

    if (!userId) {
        return (
            <section
                className="reading-status-panel"
                aria-label="Okuma durumu"
            >
                <p className="eyebrow">
                    Kişisel kitaplığın
                </p>

                <h2>Bu kitabı profiline ekle</h2>

                <p className="reading-status-description">
                    Okuma durumunu seçmek ve kişisel
                    kitaplığını oluşturmak için giriş yap.
                </p>

                <Link
                    className="button primary-button"
                    to="/login"
                >
                    Giriş Yap
                </Link>
            </section>
        )
    }

    if (loadErrorMessage) {
        return (
            <section
                className="reading-status-panel"
                aria-label="Okuma durumu"
            >
                <p
                    className="reading-status-message error"
                    role="alert"
                >
                    {loadErrorMessage}
                </p>

                <button
                    className="button secondary-button"
                    type="button"
                    onClick={() =>
                        setRequestVersion(
                            (current) => current + 1,
                        )
                    }
                >
                    Tekrar Dene
                </button>
            </section>
        )
    }

    return (
        <section
            className="reading-status-panel"
            aria-label="Okuma durumu"
        >
            <div className="reading-status-heading">
                <div>
                    <p className="eyebrow">
                        Kişisel kitaplığın
                    </p>

                    <h2>Okuma durumun</h2>
                </div>

                <strong>
                    {libraryEntry
                        ? statusLabels[
                        libraryEntry.status
                        ]
                        : 'Henüz kitaplığında değil'}
                </strong>
            </div>

            <div className="reading-status-options">
                {statusOptions.map((option) => {
                    const isSelected =
                        libraryEntry?.status ===
                        option.status

                    return (
                        <button
                            className={
                                'reading-status-button' +
                                (isSelected
                                    ? ' selected'
                                    : '')
                            }
                            type="button"
                            key={option.status}
                            aria-pressed={isSelected}
                            disabled={isSaving}
                            onClick={() =>
                                void handleStatusChange(
                                    option.status,
                                )
                            }
                        >
                            {isSelected && (
                                <span aria-hidden="true">
                                    ✓
                                </span>
                            )}

                            {option.label}
                        </button>
                    )
                })}
            </div>

            {libraryEntry && (
                <button
                    className="reading-remove-button"
                    type="button"
                    disabled={isSaving}
                    onClick={() =>
                        void handleRemove()
                    }
                >
                    Listeden çıkar
                </button>
            )}

            {isSaving && (
                <p
                    className="reading-status-message"
                    role="status"
                >
                    İşlem gerçekleştiriliyor...
                </p>
            )}

            {successMessage && !isSaving && (
                <p
                    className="reading-status-message success"
                    role="status"
                >
                    {successMessage}
                </p>
            )}

            {actionErrorMessage && !isSaving && (
                <p
                    className="reading-status-message error"
                    role="alert"
                >
                    {actionErrorMessage}
                </p>
            )}
        </section>
    )
}

export default ReadingStatusPanel