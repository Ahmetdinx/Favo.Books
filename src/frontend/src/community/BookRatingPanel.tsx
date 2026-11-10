import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookRatings, RatingApiError, removeBookRating, setBookRating } from './communityApi'
import { emptyRatingDraft, toRatingScores, previewOverallScore } from './ratingCalculations'
import type { BookRatingSummary, RatingDraft } from './communityTypes'
import './BookRatingPanel.css'
import { readRatingDraft, writeRatingDraft, clearRatingDraft, criterionHints, scoreLabel } from './ratingDraftStorage'

function draftStorage() {
    try { return window.localStorage } catch { return null }
}

type Props = { bookId: string }

export default function BookRatingPanel({ bookId }: Props) {
    const { session, isInitializing } = useAuth()
    if (isInitializing) return <p role="status">Değerlendirmeler yükleniyor…</p>

    // Remount on book/account changes so another user's draft never appears.
    return <RatingEditor key={`${bookId}:${session?.userId ?? 'guest'}`}
        bookId={bookId} userId={session?.userId} />
}

function errorMessage(error: unknown) {
    if (error instanceof RatingApiError) {
        if (error.status === 401) return 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.'
        if (error.status === 404) return 'Kitap katalogda bulunamadı.'
        if (error.status === 400) return 'Yedi kriterin tamamı için 1–10 arasında bir tam sayı seçin.'
    }
    return 'İşlem tamamlanamadı. Lütfen tekrar deneyin.'
}

function formatScore(value: number) {
    return value.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function RatingEditor({ bookId, userId }: Props & { userId?: string }) {
    const signedIn = !!userId
    const { hash } = useLocation()
    const panel = useRef<HTMLElement | null>(null)
    const followedAnchor = useRef(false)
    const authenticatedFetch = useAuthenticatedFetch()
    const [summary, setSummary] = useState<BookRatingSummary | null>(null)
    const [draft, setDraft] = useState<RatingDraft>(emptyRatingDraft)
    const [isSaving, setIsSaving] = useState(false)
    const [isLoading, setIsLoading] = useState(true)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [reload, setReload] = useState(0)
    const pendingWrite = useRef<AbortController | null>(null)
    const [draftNotice, setDraftNotice] = useState('')
    const [hasDraft, setHasDraft] = useState(false)

    function clearLocalDraft() {
        const storage = draftStorage()
        const cleared = !!storage && !!userId && clearRatingDraft(storage, userId, bookId)
        setHasDraft(!cleared)
        setDraftNotice(cleared ? '' : 'Bu tarayıcıdaki taslak silinemedi.')
    }

    function changeDraft(next: RatingDraft) {
        setDraft(next)
        setMessage('')
        const storage = draftStorage()
        const saved = !!storage && !!userId && writeRatingDraft(storage, userId, bookId, next)
        setHasDraft(true)
        setDraftNotice(saved ? 'Taslak bu tarayıcıda saklandı.' : 'Taslak saklanamadı.')
    }

    useEffect(() => {
        if (hash !== '#book-rating') { followedAnchor.current = false; return }
        if (isLoading || !summary || followedAnchor.current) return
        followedAnchor.current = true
        panel.current?.scrollIntoView({ block: 'start', behavior: 'instant' })
        panel.current?.focus({ preventScroll: true })
    }, [hash, isLoading, summary])

    useEffect(() => {
        const controller = new AbortController()
        setIsLoading(true)
        setError('')
        void getBookRatings(bookId, signedIn ? authenticatedFetch : null, controller.signal)
            .then((result) => {
                if (controller.signal.aborted) return
                setSummary(result)
                const storage = draftStorage()
                const localDraft = storage && userId ? readRatingDraft(storage, userId, bookId) : null
                setDraft(localDraft ?? result.currentUserRating?.scores ?? emptyRatingDraft())
                setHasDraft(!!localDraft)
                setDraftNotice(localDraft ? 'Yarım kalan taslağın açıldı.' : '')
            })
            .catch((failure: unknown) => {
                if (!controller.signal.aborted) setError(errorMessage(failure))
            })
            .finally(() => {
                if (!controller.signal.aborted) setIsLoading(false)
            })
        return () => controller.abort()
    }, [authenticatedFetch, bookId, signedIn, userId, reload])

    useEffect(() => () => pendingWrite.current?.abort(), [])

    async function save(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        const scores = toRatingScores(draft)
        if (!signedIn || !scores || isSaving || isLoading || pendingWrite.current) return
        const controller = new AbortController()
        pendingWrite.current = controller
        setIsSaving(true)
        setError('')
        setMessage('')
        try {
            const result = await setBookRating(bookId, scores, authenticatedFetch, controller.signal)
            if (!controller.signal.aborted) {
                setSummary(result)
                setDraft(result.currentUserRating?.scores ?? emptyRatingDraft())
                clearLocalDraft()
                setMessage('Değerlendirmen kaydedildi.')
                window.dispatchEvent(new Event('achievements-changed'))
            }
        } catch (failure) {
            if (!controller.signal.aborted) setError(errorMessage(failure))
        } finally {
            pendingWrite.current = null
            if (!controller.signal.aborted) setIsSaving(false)
        }
    }

    async function remove() {
        if (!signedIn || isSaving || isLoading || pendingWrite.current) return
        const controller = new AbortController()
        pendingWrite.current = controller
        setIsSaving(true)
        setError('')
        setMessage('')
        try {
            await removeBookRating(bookId, authenticatedFetch, controller.signal)
            if (controller.signal.aborted) return
            clearLocalDraft()
            setDraft(emptyRatingDraft())
            setSummary(current => current ? { ...current, currentUserRating: null, currentUserLegacyScore: null } : current)
            window.dispatchEvent(new Event('achievements-changed'))
            const result = await getBookRatings(bookId, authenticatedFetch, controller.signal)
            if (!controller.signal.aborted) {
                setSummary(result)
                setDraft(emptyRatingDraft())
                setMessage('Değerlendirmen kaldırıldı.')
                window.dispatchEvent(new Event('achievements-changed'))
            }
        } catch (failure) {
            if (!controller.signal.aborted) setError(errorMessage(failure))
        } finally {
            pendingWrite.current = null
            if (!controller.signal.aborted) setIsSaving(false)
        }
    }

    const preview = summary ? previewOverallScore(draft, summary.criteria) : null
    const hasRating = !!summary?.currentUserRating || summary?.currentUserLegacyScore != null
    const filled = Object.values(draft).filter(value => value !== '').length
    const scores = summary ? Array.from({ length: summary.maximumScore - summary.minimumScore + 1 }, (_, index) => summary.minimumScore + index) : []

    return (
        <section ref={panel} id="book-rating" tabIndex={-1} className="rating-card" aria-labelledby="book-rating-title" aria-busy={isLoading || isSaving}>
            <header className="rating-card-header">
                <h2 id="book-rating-title">Değerlendirme</h2>
                {summary && (
                    <p className="rating-card-community">
                        {summary.averageScore == null
                            ? <span>Henüz okur puanı yok</span>
                            : <><strong>{formatScore(summary.averageScore)}</strong><span> / 10 · {summary.ratingCount} okur</span></>}
                    </p>
                )}
            </header>

            {isLoading && <p className="rating-card-quiet" role="status">Değerlendirmeler yükleniyor…</p>}
            {error && <p className="rating-card-error" role="alert">{error}</p>}
            {!summary && !isLoading && (
                <button className="button secondary-button" type="button" onClick={() => setReload((value) => value + 1)}>
                    Tekrar dene
                </button>
            )}

            {summary && !isLoading && (
                <>
                    {summary.ratingCount > 0 && <details className="rating-community-details"><summary>Okur ortalamaları</summary>
                        <dl className="rating-averages" aria-label="Kriterlere göre okur ortalamaları">
                            {summary.criteria.map((criterion) => {
                                const average = summary.criteriaAverages?.[criterion.key]
                                return (
                                    <div key={criterion.key}>
                                        <dt>{criterion.label}</dt>
                                        <dd>
                                            <span className="rating-average-bar" aria-hidden="true">
                                                <span style={{ width: `${((average ?? 0) / summary.maximumScore) * 100}%` }} />
                                            </span>
                                            <span>{average == null ? '—' : formatScore(average)}</span>
                                        </dd>
                                    </div>
                                )
                            })}
                        </dl>
                    </details>}
                    {summary.legacyRatingCount > 0 && (
                        <p className="rating-card-quiet">
                            {summary.legacyRatingCount} eski tek puanlı değerlendirme ortalamaya dahil değil.
                        </p>
                    )}

                    {signedIn ? (
                        <form className="rating-form" onSubmit={(event) => void save(event)}>
                            <fieldset disabled={isSaving || isLoading}>
                                <legend className="rating-visually-hidden">{hasRating ? 'Değerlendirmeni güncelle' : 'Kitabı yedi kriterle puanla'}</legend>
                                {summary.currentUserLegacyScore != null && (
                                    <p className="rating-card-quiet">
                                        Önceki puanın {summary.currentUserLegacyScore} / 5. Yedi kriteri doldurunca yenisiyle değişir.
                                    </p>
                                )}
                                <div className="rating-rows">
                                    {summary.criteria.map((criterion) => {
                                        const value = draft[criterion.key]
                                        return (
                                            <div className="rating-row" key={criterion.key} role="radiogroup"
                                                aria-labelledby={`rating-label-${criterion.key}`} aria-describedby={`rating-help-${criterion.key}`}>
                                                <span className="rating-row-label" id={`rating-label-${criterion.key}`}
                                                    title={`${criterionHints[criterion.key]} (ağırlık %${criterion.weight})`}>
                                                    {criterion.label}
                                                </span>
                                                <span id={`rating-help-${criterion.key}`} className="rating-visually-hidden">{criterionHints[criterion.key]}</span>
                                                <span className="rating-scale">
                                                    {scores.map((score) => (
                                                        <label key={score} className={value !== '' && score <= value ? 'filled' : undefined}
                                                            title={`${score} · ${scoreLabel(score)}`}>
                                                            <input type="radio" name={`rating-${criterion.key}`} value={score} required
                                                                checked={value === score}
                                                                aria-label={`${score} · ${scoreLabel(score)}`}
                                                                onChange={() => changeDraft({ ...draft, [criterion.key]: score })} />
                                                            <span aria-hidden="true" />
                                                        </label>
                                                    ))}
                                                </span>
                                                <span className="rating-row-value" aria-hidden="true">{value}</span>
                                            </div>
                                        )
                                    })}
                                </div>

                                <div className="rating-footer">
                                    <div className="rating-overall" role="status">
                                        <strong>{preview == null ? `${filled}/7` : formatScore(preview)}</strong>
                                        <span>{preview == null ? 'kriter puanlandı' : 'genel puanın'}</span>
                                    </div>
                                    {draftNotice && <p className="rating-card-quiet rating-draft-notice">{draftNotice}</p>}
                                    <div className="rating-actions">
                                        {hasDraft && <button className="rating-link-button" type="button" onClick={() => {
                                            clearLocalDraft()
                                            setDraft(summary.currentUserRating?.scores ?? emptyRatingDraft())
                                            setMessage('')
                                        }}>Taslağı sil</button>}
                                        {hasRating && <button className="rating-link-button" type="button"
                                            onClick={() => void remove()}>Kaldır</button>}
                                        <button className="button primary-button" type="submit" disabled={preview == null || isSaving}>
                                            {isSaving ? 'Kaydediliyor…' : hasRating ? 'Güncelle' : 'Kaydet'}
                                        </button>
                                    </div>
                                </div>
                            </fieldset>
                        </form>
                    ) : (
                        <p className="rating-card-quiet"><Link to="/login">Giriş yap</Link>, kitabı yedi kriterle puanla.</p>
                    )}
                </>
            )}
            {message && <p className="rating-card-success" role="status">{message}</p>}
        </section>
    )
}
