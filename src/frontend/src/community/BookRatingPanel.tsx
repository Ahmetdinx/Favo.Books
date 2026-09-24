import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookRatings, RatingApiError, removeBookRating, setBookRating } from './communityApi'
import { emptyRatingDraft, toRatingScores, previewOverallScore } from './ratingCalculations'
import type { BookRatingSummary, RatingDraft } from './communityTypes'
import './BookRatingPanel.css'

type Props = { bookId: string }

export default function BookRatingPanel({ bookId }: Props) {
    const { session, isInitializing } = useAuth()
    if (isInitializing) return <p role="status">Değerlendirmeler yükleniyor…</p>

    // Remount on book/account changes so another user's draft never appears.
    return <RatingEditor key={`${bookId}:${session?.userId ?? 'guest'}`}
        bookId={bookId} signedIn={!!session} />
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

function RatingEditor({ bookId, signedIn }: Props & { signedIn: boolean }) {
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
                setDraft(result.currentUserRating?.scores ?? emptyRatingDraft())
            })
            .catch((failure: unknown) => {
                if (!controller.signal.aborted) setError(errorMessage(failure))
            })
            .finally(() => {
                if (!controller.signal.aborted) setIsLoading(false)
            })
        return () => controller.abort()
    }, [authenticatedFetch, bookId, signedIn, reload])

    useEffect(() => () => pendingWrite.current?.abort(), [])

    async function save(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        const scores = toRatingScores(draft)
        if (!signedIn || !scores || isSaving || isLoading) return
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
                setMessage('Yedi kriterden oluşan değerlendirmeniz kaydedildi.')
                window.dispatchEvent(new Event('achievements-changed'))
            }
        } catch (failure) {
            if (!controller.signal.aborted) setError(errorMessage(failure))
        } finally {
            if (!controller.signal.aborted) setIsSaving(false)
        }
    }

    async function remove() {
        if (!signedIn || isSaving || isLoading) return
        const controller = new AbortController()
        pendingWrite.current = controller
        setIsSaving(true)
        setError('')
        setMessage('')
        try {
            await removeBookRating(bookId, authenticatedFetch, controller.signal)
            const result = await getBookRatings(bookId, authenticatedFetch, controller.signal)
            if (!controller.signal.aborted) {
                setSummary(result)
                setDraft(emptyRatingDraft())
                setMessage('Değerlendirmeniz kaldırıldı.')
                window.dispatchEvent(new Event('achievements-changed'))
            }
        } catch (failure) {
            if (!controller.signal.aborted) setError(errorMessage(failure))
        } finally {
            if (!controller.signal.aborted) setIsSaving(false)
        }
    }

    const preview = summary ? previewOverallScore(draft, summary.criteria) : null
    const hasRating = !!summary?.currentUserRating || summary?.currentUserLegacyScore != null

    return (
        <section ref={panel} id="book-rating" tabIndex={-1} className="detailed-rating" aria-labelledby="book-rating-title" aria-busy={isLoading || isSaving}>
            <header className="detailed-rating-header">
                <div>
                    <p className="eyebrow">Okurların değerlendirmesi</p>
                    <h2 id="book-rating-title">Bir kitabın yedi yönü</h2>
                    <p>Her kriter 10 üzerinden değerlendirilir. Genel puan, aşağıdaki ağırlıklarla hesaplanır.</p>
                </div>
                {summary && (
                    <div className="detailed-rating-overall">
                        <strong>{summary.averageScore == null ? '—' : formatScore(summary.averageScore)}</strong>
                        <span>/ 10 genel puan</span>
                        <small>{summary.ratingCount} ayrıntılı değerlendirme</small>
                    </div>
                )}
            </header>

            {isLoading && <p role="status">Değerlendirmeler yükleniyor…</p>}
            {error && <p className="detailed-rating-error" role="alert">{error}</p>}
            {!summary && !isLoading && (
                <button className="button secondary-button" type="button" onClick={() => setReload((value) => value + 1)}>
                    Tekrar dene
                </button>
            )}

            {summary && !isLoading && (
                <>
                    <dl className="detailed-rating-averages" aria-label="Kriterlere göre okur ortalamaları">
                        {summary.criteria.map((criterion) => {
                            const average = summary.criteriaAverages?.[criterion.key]
                            return (
                                <div key={criterion.key}>
                                    <dt>{criterion.label} <small>%{criterion.weight}</small></dt>
                                    <dd>
                                        {average != null && <meter min={0} max={summary.maximumScore} value={average}
                                            aria-label={`${criterion.label} ortalaması`} />}
                                        <span>{average == null ? 'Henüz puan yok' : `${formatScore(average)} / 10`}</span>
                                    </dd>
                                </div>
                            )
                        })}
                    </dl>
                    {summary.ratingCount === 0 && <p>Bu kitabı yedi kriterle ilk değerlendiren siz olun.</p>}
                    {summary.legacyRatingCount > 0 && (
                        <p className="detailed-rating-note">
                            Eski sistemdeki {summary.legacyRatingCount} tek puanlı değerlendirme, kriter puanları bulunmadığı için bu ortalamalara dahil değildir.
                        </p>
                    )}

                    {signedIn ? (
                        <form onSubmit={(event) => void save(event)}>
                            <fieldset disabled={isSaving || isLoading}>
                                <legend>{hasRating ? 'Değerlendirmenizi güncelleyin' : 'Sizin değerlendirmeniz'}</legend>
                                {summary.currentUserLegacyScore != null && (
                                    <p className="detailed-rating-note">
                                        Önceki puanınız {summary.currentUserLegacyScore} / 5. Yedi kriteri doldurduğunuzda yeni değerlendirmenizle değiştirilecek.
                                    </p>
                                )}
                                {summary.criteria.map((criterion) => (
                                    <div className="detailed-rating-field" key={criterion.key}>
                                        <label htmlFor={`rating-${criterion.key}`}>
                                            {criterion.label} <small>%{criterion.weight}</small>
                                        </label>
                                        <p id={`rating-help-${criterion.key}`}>{criterion.description}</p>
                                        <select id={`rating-${criterion.key}`} required
                                            aria-describedby={`rating-help-${criterion.key}`}
                                            value={draft[criterion.key]}
                                            onChange={(event) => {
                                                const value = event.target.value === '' ? '' : Number(event.target.value)
                                                setDraft((current) => ({ ...current, [criterion.key]: value }))
                                                setMessage('')
                                            }}>
                                            <option value="">Puan seçin</option>
                                            {Array.from({ length: summary.maximumScore - summary.minimumScore + 1 },
                                                (_, index) => summary.minimumScore + index).map((score) => (
                                                <option key={score} value={score}>{score} / {summary.maximumScore}</option>
                                            ))}
                                        </select>
                                    </div>
                                ))}
                                <div className="detailed-rating-preview" role="status">
                                    {preview == null ? 'Genel puan için yedi kriteri de doldurun.' : `Hesaplanan genel puanınız: ${formatScore(preview)} / 10`}
                                </div>
                                <div className="detailed-rating-actions">
                                    <button className="button primary-button" type="submit" disabled={preview == null || isSaving}>
                                        {isSaving ? 'Kaydediliyor…' : hasRating ? 'Değerlendirmeyi güncelle' : 'Değerlendirmeyi kaydet'}
                                    </button>
                                    {hasRating && <button className="detailed-rating-remove" type="button"
                                        onClick={() => void remove()}>Değerlendirmemi kaldır</button>}
                                </div>
                            </fieldset>
                        </form>
                    ) : (
                        <p><Link to="/login">Giriş yapın</Link> ve kitabı yedi kriterle değerlendirin.</p>
                    )}
                </>
            )}
            {message && <p className="detailed-rating-success" role="status">{message}</p>}
        </section>
    )
}
