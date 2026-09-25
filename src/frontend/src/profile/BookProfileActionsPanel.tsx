import {
    useEffect,
    useState,
} from 'react'

import { useAuth } from '../auth/useAuth'
import {
    useAuthenticatedFetch,
} from '../auth/useAuthenticatedFetch'

import {
    getBedsideBook,
    getInfluentialBooks,
    ProfileApiError,
    removeBedsideBook,
    removeInfluentialBook,
    setBedsideBook,
    setInfluentialBook,
} from './profileApi'

import type {
    BedsideBookSelection,
    InfluentialBookSelection,
} from './profileTypes'

type BookProfileActionsPanelProps = {
    bookId: string
}

const influentialPositions = [
    1,
    2,
    3,
    4,
    5,
] as const

function getActionErrorMessage(
    error: unknown,
    isBookSelection = false,
) {
    if (error instanceof ProfileApiError) {
        if (error.status === 401) {
            return (
                'Oturumunuz sona erdi. ' +
                'Lütfen tekrar giriş yapın.'
            )
        }

        if (error.status === 409) {
            return (
                'Bu kitap etkileyen ' +
                'kitaplarınızda zaten bulunuyor.'
            )
        }

        if (error.status === 404) {
            if (isBookSelection) {
                return 'Bu kitap katalogda bulunamadı. Lütfen kataloğa dönüp başka bir kitap seçin.'
            }

            return (
                'Profil seçimi bulunamadı. ' +
                'Bilgiler yenileniyor.'
            )
        }
    }

    return (
        'İşlem tamamlanamadı. ' +
        'Lütfen tekrar deneyin.'
    )
}

function BookProfileActionsPanel({
    bookId,
}: BookProfileActionsPanelProps) {
    const {
        session,
        isInitializing,
    } = useAuth()

    const authenticatedFetch =
        useAuthenticatedFetch()

    const userId =
        session?.userId ?? null

    const [
        bedsideBook,
        setBedsideBookState,
    ] = useState<
        BedsideBookSelection | null
    >(null)

    const [
        influentialBooks,
        setInfluentialBooks,
    ] = useState<
        InfluentialBookSelection[]
    >([])

    const [
        bedsideNote,
        setBedsideNote,
    ] = useState('')

    const [
        influentialReflection,
        setInfluentialReflection,
    ] = useState('')

    const [
        selectedPosition,
        setSelectedPosition,
    ] = useState(1)

    const [isLoading, setIsLoading] =
        useState(false)

    const [
        isBedsideSaving,
        setIsBedsideSaving,
    ] = useState(false)

    const [
        isInfluentialSaving,
        setIsInfluentialSaving,
    ] = useState(false)

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
            setBedsideBookState(null)
            setInfluentialBooks([])
            setIsLoading(false)
            return
        }

        const controller =
            new AbortController()

        async function loadProfileSelections() {
            setIsLoading(true)
            setLoadErrorMessage('')
            setActionErrorMessage('')
            setSuccessMessage('')

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

                setBedsideBookState(
                    bedsideResponse,
                )

                setInfluentialBooks(
                    influentialResponse.items,
                )

                const bedsideIsCurrent =
                    bedsideResponse?.bookId ===
                    bookId

                setBedsideNote(
                    bedsideIsCurrent
                        ? bedsideResponse
                            ?.personalNote ?? ''
                        : '',
                )

                const currentInfluential =
                    influentialResponse.items
                        .find(
                            (item) =>
                                item.bookId ===
                                bookId,
                        )

                setInfluentialReflection(
                    currentInfluential
                        ?.personalReflection ??
                    '',
                )

                const firstAvailablePosition =
                    influentialPositions.find(
                        (position) =>
                            !influentialResponse
                                .items
                                .some(
                                    (item) =>
                                        item.position ===
                                        position,
                                ),
                    )

                setSelectedPosition(
                    currentInfluential
                        ?.position ??
                    firstAvailablePosition ??
                    1,
                )
            } catch {
                if (
                    !controller.signal.aborted
                ) {
                    setLoadErrorMessage(
                        'Profil seçimleriniz ' +
                        'yüklenemedi.',
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

        void loadProfileSelections()

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

    const bedsideIsCurrent =
        bedsideBook?.bookId === bookId

    const currentInfluentialBook =
        influentialBooks.find(
            (item) =>
                item.bookId === bookId,
        ) ?? null

    const selectedPositionBook =
        influentialBooks.find(
            (item) =>
                item.position ===
                selectedPosition,
        ) ?? null

    const isSaving =
        isBedsideSaving ||
        isInfluentialSaving

    const selectionSummary = [
        bedsideIsCurrent
            ? 'Başucu kitabın'
            : null,
        currentInfluentialBook
            ? `${currentInfluentialBook.position}. etkileyen kitabın`
            : null,
    ]
        .filter(Boolean)
        .join(' • ')

    async function handleSetBedsideBook() {
        if (!userId || isSaving) {
            return
        }

        setIsBedsideSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            const response =
                await setBedsideBook(
                    authenticatedFetch,
                    {
                        bookId,
                        personalNote:
                            bedsideNote.trim() ||
                            null,
                    },
                )

            setBedsideBookState(response)
            setBedsideNote(
                response.personalNote ?? '',
            )

            setSuccessMessage(
                bedsideIsCurrent
                    ? 'Başucu kitabı notunuz güncellendi.'
                    : 'Kitap başucu kitabınız olarak seçildi.',
            )
        } catch (error) {
            setActionErrorMessage(
                getActionErrorMessage(error, true),
            )
        } finally {
            setIsBedsideSaving(false)
        }
    }

    async function
        handleRemoveBedsideBook() {
        if (
            !userId ||
            !bedsideIsCurrent ||
            isSaving
        ) {
            return
        }

        setIsBedsideSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            await removeBedsideBook(
                authenticatedFetch,
            )

            setBedsideBookState(null)
            setBedsideNote('')

            setSuccessMessage(
                'Başucu kitabı seçimi kaldırıldı.',
            )
        } catch (error) {
            if (
                error instanceof
                ProfileApiError &&
                error.status === 404
            ) {
                setRequestVersion(
                    (current) => current + 1,
                )
            }

            setActionErrorMessage(
                getActionErrorMessage(error),
            )
        } finally {
            setIsBedsideSaving(false)
        }
    }

    async function
        handleSetInfluentialBook() {
        if (!userId || isSaving) {
            return
        }

        setIsInfluentialSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            const response =
                await setInfluentialBook(
                    authenticatedFetch,
                    selectedPosition,
                    {
                        bookId,
                        personalReflection:
                            influentialReflection
                                .trim() ||
                            null,
                    },
                )

            setInfluentialBooks(
                (current) =>
                    [
                        ...current.filter(
                            (item) =>
                                item.position !==
                                response.position,
                        ),
                        response,
                    ].sort(
                        (first, second) =>
                            first.position -
                            second.position,
                    ),
            )

            setSelectedPosition(
                response.position,
            )

            setInfluentialReflection(
                response
                    .personalReflection ??
                '',
            )

            setSuccessMessage(
                currentInfluentialBook
                    ? 'Kişisel düşünceniz güncellendi.'
                    : 'Kitap, sizi etkileyen kitaplara eklendi.',
            )
        } catch (error) {
            setActionErrorMessage(
                getActionErrorMessage(error, true),
            )
        } finally {
            setIsInfluentialSaving(false)
        }
    }

    async function
        handleRemoveInfluentialBook() {
        if (
            !userId ||
            !currentInfluentialBook ||
            isSaving
        ) {
            return
        }

        setIsInfluentialSaving(true)
        setActionErrorMessage('')
        setSuccessMessage('')

        try {
            await removeInfluentialBook(
                authenticatedFetch,
                currentInfluentialBook
                    .position,
            )

            setInfluentialBooks(
                (current) =>
                    current.filter(
                        (item) =>
                            item
                                .influentialBookId !==
                            currentInfluentialBook
                                .influentialBookId,
                    ),
            )

            setInfluentialReflection('')

            const firstAvailablePosition =
                influentialPositions.find(
                    (position) =>
                        position ===
                        currentInfluentialBook
                            .position ||
                        !influentialBooks.some(
                            (item) =>
                                item.position ===
                                position,
                        ),
                )

            setSelectedPosition(
                firstAvailablePosition ?? 1,
            )

            setSuccessMessage(
                'Kitap, sizi etkileyen ' +
                'kitaplardan çıkarıldı.',
            )
        } catch (error) {
            if (
                error instanceof
                ProfileApiError &&
                error.status === 404
            ) {
                setRequestVersion(
                    (current) => current + 1,
                )
            }

            setActionErrorMessage(
                getActionErrorMessage(error),
            )
        } finally {
            setIsInfluentialSaving(false)
        }
    }

    if (
        isInitializing ||
        !userId
    ) {
        return null
    }

    if (isLoading) {
        return (
            <section className="profile-book-actions">
                <p
                    className="profile-book-actions-message"
                    role="status"
                >
                    Profil seçimleriniz
                    yükleniyor...
                </p>
            </section>
        )
    }

    if (loadErrorMessage) {
        return (
            <section className="profile-book-actions">
                <p
                    className="profile-book-actions-message error"
                    role="alert"
                >
                    {loadErrorMessage}
                </p>

                <button
                    className={
                        'button secondary-button'
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
            </section>
        )
    }

    return (
        <details className="profile-book-actions">
            <summary>
                <div>
                    <p className="eyebrow">
                        Okur kimliğin
                    </p>

                    <strong>
                        Profilinde öne çıkar
                    </strong>
                </div>

                <span>
                    {selectionSummary ||
                        'Başucu veya etkileyen seçimi yap'}
                </span>
            </summary>

            <div className="profile-book-actions-content">
                <section className="profile-feature-editor">
                    <header>
                        <div>
                            <span>
                                Özel alan
                            </span>

                            <h3>
                                Başucu kitabım
                            </h3>
                        </div>

                        <strong>
                            Tek kitap
                        </strong>
                    </header>

                    {bedsideBook &&
                        !bedsideIsCurrent && (
                            <p className="profile-feature-notice">
                                Başka bir başucu
                                kitabınız seçili. Bu
                                kitabı seçerseniz
                                önceki kitabın yerini
                                alır.
                            </p>
                        )}

                    <label>
                        <span>
                            Neden başucunuzda?
                        </span>

                        <textarea
                            value={bedsideNote}
                            maxLength={500}
                            rows={4}
                            disabled={isSaving}
                            placeholder={
                                'Bu kitabın sizin ' +
                                'için neden özel ' +
                                'olduğunu anlatın...'
                            }
                            onChange={(event) =>
                                setBedsideNote(
                                    event.target
                                        .value,
                                )
                            }
                        />
                    </label>

                    <div className="profile-feature-actions">
                        <button
                            className={
                                'button ' +
                                'primary-button'
                            }
                            type="button"
                            disabled={isSaving}
                            onClick={() =>
                                void handleSetBedsideBook()
                            }
                        >
                            {bedsideIsCurrent
                                ? 'Başucu Seçimini Güncelle'
                                : 'Başucu Kitabım Yap'}
                        </button>

                        {bedsideIsCurrent && (
                            <button
                                className="profile-feature-remove"
                                type="button"
                                disabled={isSaving}
                                onClick={() =>
                                    void handleRemoveBedsideBook()
                                }
                            >
                                Başucu seçiminden
                                kaldır
                            </button>
                        )}
                    </div>
                </section>

                <section className="profile-feature-editor">
                    <header>
                        <div>
                            <span>
                                Kişisel iz
                            </span>

                            <h3>
                                Beni en çok
                                etkileyen
                            </h3>
                        </div>

                        <strong>
                            {
                                influentialBooks
                                    .length
                            }
                            /5 kitap
                        </strong>
                    </header>

                    <label>
                        <span>Sırası</span>

                        <select
                            value={
                                selectedPosition
                            }
                            disabled={
                                isSaving ||
                                Boolean(
                                    currentInfluentialBook,
                                )
                            }
                            onChange={(event) =>
                                setSelectedPosition(
                                    Number(
                                        event.target
                                            .value,
                                    ),
                                )
                            }
                        >
                            {influentialPositions.map(
                                (position) => {
                                    const occupant =
                                        influentialBooks
                                            .find(
                                                (
                                                    item,
                                                ) =>
                                                    item.position ===
                                                    position,
                                            )

                                    const suffix =
                                        occupant
                                            ? occupant.bookId ===
                                                bookId
                                                ? ' — Bu kitap'
                                                : ' — Dolu'
                                            : ''

                                    return (
                                        <option
                                            key={
                                                position
                                            }
                                            value={
                                                position
                                            }
                                        >
                                            {
                                                position
                                            }
                                            . sıra
                                            {suffix}
                                        </option>
                                    )
                                },
                            )}
                        </select>
                    </label>

                    {currentInfluentialBook && (
                        <p className="profile-feature-notice">
                            Sırayı değiştirmek için
                            kitabı önce bu alandan
                            kaldırın.
                        </p>
                    )}

                    {!currentInfluentialBook &&
                        selectedPositionBook && (
                            <p className="profile-feature-notice">
                                Seçtiğiniz sıra dolu.
                                Kaydettiğinizde bu
                                kitap önceki kitabın
                                yerini alır.
                            </p>
                        )}

                    <label>
                        <span>
                            Bende ne bıraktı?
                        </span>

                        <textarea
                            value={
                                influentialReflection
                            }
                            maxLength={1000}
                            rows={5}
                            disabled={isSaving}
                            placeholder={
                                'Bu kitabın sizi ' +
                                'nasıl etkilediğini ' +
                                'anlatın...'
                            }
                            onChange={(event) =>
                                setInfluentialReflection(
                                    event.target
                                        .value,
                                )
                            }
                        />
                    </label>

                    <div className="profile-feature-actions">
                        <button
                            className={
                                'button ' +
                                'primary-button'
                            }
                            type="button"
                            disabled={isSaving}
                            onClick={() =>
                                void handleSetInfluentialBook()
                            }
                        >
                            {currentInfluentialBook
                                ? 'Düşüncemi Güncelle'
                                : 'Etkileyenlere Ekle'}
                        </button>

                        {currentInfluentialBook && (
                            <button
                                className="profile-feature-remove"
                                type="button"
                                disabled={isSaving}
                                onClick={() =>
                                    void handleRemoveInfluentialBook()
                                }
                            >
                                Etkileyenlerden
                                kaldır
                            </button>
                        )}
                    </div>
                </section>

                {isSaving && (
                    <p
                        className="profile-book-actions-message"
                        role="status"
                    >
                        Seçiminiz kaydediliyor...
                    </p>
                )}

                {successMessage &&
                    !isSaving && (
                        <p
                            className="profile-book-actions-message success"
                            role="status"
                        >
                            {successMessage}
                        </p>
                    )}

                {actionErrorMessage &&
                    !isSaving && (
                        <p
                            className="profile-book-actions-message error"
                            role="alert"
                        >
                            {actionErrorMessage}
                        </p>
                    )}
            </div>
        </details>
    )
}

export default BookProfileActionsPanel
