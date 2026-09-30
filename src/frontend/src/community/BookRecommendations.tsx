import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { saveInterestedBook } from '../reading/readingApi'
import './BookRecommendations.css'

const criteria = [
    ['theme', 'Konu / Fikir'], ['language', 'Anlatım ve Dil'], ['characters', 'Karakterler'],
    ['structure', 'Kurgu ve Yapı'], ['pace', 'Tempo / Sürükleyicilik'],
    ['impact', 'Duygusal / Düşünsel Etki'], ['experience', 'Genel Okuma Deneyimi'],
] as const
type Preferences = { categories: string[]; criteria: string[]; hasSavedPreferences: boolean; availableCategories: { slug: string; name: string }[] }
type FeedbackItem = { bookId: string; title: string; choice: 'interested' | 'dismissed' }
type Recommendations = { hasPersonalSignals: boolean; feedback: FeedbackItem[]; items: {
    bookId: string; title: string; coverImageUrl: string; reasons: string[]; ratingCount: number
    shelf: string; genres: string[]; priorityScores: Record<string, number>
}[] }

export default function BookRecommendations({ home = false }: { home?: boolean }) {
    const { session } = useAuth()
    const [preview, setPreview] = useState(false)
    return session ? <>
        {import.meta.env.DEV && <div className="recommendation-preview-toggle"><button type="button" onClick={() => setPreview(value => !value)}>{preview ? 'Gerçek önerilere dön' : 'Test verileriyle önerileri dene'}</button></div>}
        <PersonalRecommendations key={session.userId + ':' + preview} home={home} preview={import.meta.env.DEV && preview} />
    </> : null
}

function PersonalRecommendations({ home, preview }: { home: boolean; preview: boolean }) {
    const Heading = home ? 'h1' : 'h2'
    const authenticatedFetch = useAuthenticatedFetch()
    const previewChoices = useRef<Record<string, FeedbackItem['choice']>>({})
    const request = useCallback(async (input: string | URL, init?: RequestInit): Promise<Response> => {
        const url = String(input)
        if (!preview) return authenticatedFetch(url, init)
        if (url.endsWith('/reading-preferences')) return Response.json({
            categories: ['klasikler'], criteria: ['language', 'characters', 'pace'], hasSavedPreferences: true,
            availableCategories: [{ slug: 'klasikler', name: 'Klasikler' }],
        })
        if (url.endsWith('/feedback')) {
            const id = url.split('/').at(-2)!
            if (init?.method === 'DELETE') delete previewChoices.current[id]
            else previewChoices.current[id] = JSON.parse(String(init?.body)).choice
            return new Response(null, { status: 204 })
        }
        return authenticatedFetch('/api/community/me/recommendations/preview', {
            method: 'POST', signal: init?.signal, headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ excluded: Object.keys(previewChoices.current) }),
        })
    }, [authenticatedFetch, preview])
    const [preferences, setPreferences] = useState<Preferences | null>(null)
    const [savedPreferences, setSavedPreferences] = useState<Preferences | null>(null)
    const [editingPreferences, setEditingPreferences] = useState(false)
    const preferencesId = useId()
    const editButton = useRef<HTMLButtonElement>(null)
    const [result, setResult] = useState<Recommendations | null>(null)
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [retry, setRetry] = useState(0)
    const feedbackPending = useRef(false)
    const saveRequest = useRef<AbortController | null>(null)
    useEffect(() => () => saveRequest.current?.abort(), [])

    useEffect(() => {
        const controller = new AbortController()
        setLoading(true)
        setError('')
        async function load() {
            try {
                const responses = await Promise.all([
                    request('/api/community/me/reading-preferences', { signal: controller.signal, cache: 'no-store' }),
                    request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' }),
                ])
                if (responses.some(response => !response.ok)) throw new Error()
                const [prefs, recommendations] = await Promise.all(responses.map(response => response.json()))
                if (!controller.signal.aborted) {
                    setPreferences(prefs); setSavedPreferences(prefs); setResult(recommendations)
                    setEditingPreferences(!prefs.hasSavedPreferences)
                }
            } catch { if (!controller.signal.aborted) setError('Önerilerin yüklenemedi. Lütfen tekrar dene.') }
            finally { if (!controller.signal.aborted) setLoading(false) }
        }
        void load()
        return () => controller.abort()
    }, [request, retry])

    function toggle(key: 'categories' | 'criteria', value: string) {
        setMessage('')
        setPreferences(current => current ? { ...current, [key]: current[key].includes(value)
            ? current[key].filter(item => item !== value) : [...current[key], value] } : current)
    }

    async function refreshRecommendations() {
        if (saving) return
        const controller = new AbortController()
        saveRequest.current = controller
        setSaving(true); setError(''); setMessage('')
        try {
            const response = await request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            if (!response.ok) throw new Error()
            const data = await response.json() as Recommendations
            if (!controller.signal.aborted) { setResult(data); setMessage('Önerilerin yenilendi.') }
        } catch { if (!controller.signal.aborted) setError('Öneriler yenilenemedi. Tekrar deneyebilirsin.') }
        finally { if (!controller.signal.aborted) setSaving(false) }
    }

    async function save(event: FormEvent) {
        event.preventDefault()
        if (!preferences || saving) return
        const controller = new AbortController()
        saveRequest.current = controller
        setSaving(true); setError(''); setMessage('')
        let saved = false
        try {
            const response = await request('/api/community/me/reading-preferences', {
                method: 'PUT', headers: { 'Content-Type': 'application/json' }, signal: controller.signal,
                body: JSON.stringify({ categories: preferences.categories, criteria: preferences.criteria }),
            })
            if (!response.ok) throw new Error()
            saved = true
            if (!preview) window.dispatchEvent(new Event('reading-preferences-changed'))
            if (controller.signal.aborted) return
            const savedPrefs = { ...preferences, hasSavedPreferences: true }
            setPreferences(savedPrefs); setSavedPreferences(savedPrefs); setEditingPreferences(false)
            const updated = await request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            if (!updated.ok) throw new Error()
            const data = await updated.json() as Recommendations
            if (!controller.signal.aborted) { setResult(data); setMessage('Tercihlerin kaydedildi, önerilerin güncellendi.') }
        } catch { if (!controller.signal.aborted) setError(saved
            ? 'Tercihlerin kaydedildi fakat öneriler yenilenemedi. Tekrar deneyebilirsin.'
            : 'Tercihlerin kaydedilemedi. Seçimlerin burada duruyor; tekrar deneyebilirsin.') }
        finally {
            if (!controller.signal.aborted) {
                setSaving(false)
                if (saved) requestAnimationFrame(() => editButton.current?.focus())
            }
        }
    }

    async function giveFeedback(bookId: string, title: string, choice: FeedbackItem['choice'] | null) {
        if (saving || feedbackPending.current) return
        feedbackPending.current = true
        const controller = new AbortController()
        saveRequest.current = controller
        setSaving(true); setError(''); setMessage('')
        let saved = false
        let librarySaved = false
        try {
            if (choice === 'interested' && !preview) {
                await saveInterestedBook(request, bookId, controller.signal)
                librarySaved = true
            }
            const response = await request(`/api/community/me/recommendations/${bookId}/feedback`, {
                method: choice ? 'PUT' : 'DELETE', signal: controller.signal,
                headers: { 'Content-Type': 'application/json' },
                ...(choice ? { body: JSON.stringify({ choice }) } : {}),
            })
            if (!response.ok) throw new Error()
            saved = true
            if (controller.signal.aborted) return
            // Preserve an undo control even when the subsequent refresh fails.
            setResult(current => current ? { ...current,
                items: choice ? current.items.filter(b => b.bookId !== bookId) : current.items,
                feedback: [...current.feedback.filter(f => f.bookId !== bookId), ...(choice ? [{ bookId, title, choice }] : [])],
            } : current)
            const refreshed = await request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            if (!refreshed.ok) throw new Error()
            const data = await refreshed.json() as Recommendations
            if (!controller.signal.aborted) {
                setResult(data)
                setMessage(choice === 'dismissed' ? `«${title}» önerilerinden çıkarıldı.`
                    : choice === 'interested' ? preview ? `«${title}» test listesinden çıkarıldı. Gerçek kitaplığın değişmedi.` : `«${title}» okuma listene eklendi. Yeni öneriler hazır.` : 'Geri bildirimin kaldırıldı. Kitaplığın değişmedi.')
            }
        } catch { if (!controller.signal.aborted) setError(saved
            ? 'Geri bildirimin kaydedildi ancak öneriler yenilenemedi. Önerileri yenileyebilirsin.'
            : librarySaved ? 'Kitap kitaplığında, ancak geri bildirimin kaydedilemedi. Tekrar dene.'
                : 'İşlem tamamlanamadı. Lütfen tekrar dene.') }
        finally { if (!controller.signal.aborted) setSaving(false); saveRequest.current = null; feedbackPending.current = false }
    }

    return <section className="personal-discovery" aria-labelledby="discovery-heading" aria-busy={loading || saving}>
        {preview && <aside className="recommendation-preview-note"><strong>Test ön izlemesi · Gerçek puanlar değildir</strong><p>Klasikler + anlatım, karakterler ve tempo. “Sınır testi” 9 puanla, “Az veri” iki değerlendirmeyle güçlü eşleşmeye girmez. Butonlar yalnızca bu ön izlemeyi değiştirir.</p><button type="button" disabled={saving} onClick={() => { previewChoices.current = {}; setRetry(value => value + 1) }}>Testi sıfırla</button></aside>}
        <Heading id="discovery-heading">Şimdi ne okumalı?</Heading>
        {loading && <p role="status">Öneriler yükleniyor…</p>}
        {error && <p role="alert">{error} {!preferences && <button type="button" onClick={() => setRetry(n => n + 1)}>Tekrar dene</button>}</p>}
        {message && <p role="status">{message}</p>}
        {!loading && preferences && <div className="reading-preferences">
            <div className="reading-preferences-header">
                <div className="reading-preferences-overview">
                    <h3>Okuma tercihlerim</h3>
                    <p>{savedPreferences?.categories.length
                        ? savedPreferences.availableCategories.filter(c => savedPreferences.categories.includes(c.slug)).map(c => c.name).join(' · ')
                        : savedPreferences?.hasSavedPreferences ? 'Tüm roman türlerine açığım' : 'Sevdiğin roman türlerini seç'}
                        {!!savedPreferences?.criteria.length && ` · ${savedPreferences.criteria.length} okuma önceliği`}</p>
                </div>
                <button ref={editButton} type="button" className="preferences-edit" disabled={saving || preview}
                    aria-expanded={editingPreferences} aria-controls={preferencesId}
                    onClick={() => { setPreferences(savedPreferences); setEditingPreferences(open => !open); setMessage('') }}>
                    {editingPreferences ? 'Vazgeç' : savedPreferences?.hasSavedPreferences ? 'Düzenle' : 'Tercih seç'}
                </button>
            </div>
            <form id={preferencesId} onSubmit={save} hidden={!editingPreferences}>
                <fieldset disabled={saving}>
                    <legend>Roman türleri <small>En fazla 5</small></legend>
                    <div className="preference-options">{preferences.availableCategories.map(category => <label key={category.slug}>
                        <input type="checkbox" checked={preferences.categories.includes(category.slug)}
                            disabled={!preferences.categories.includes(category.slug) && preferences.categories.length >= 5}
                            onChange={() => toggle('categories', category.slug)} />{category.name}
                    </label>)}</div>
                </fieldset>
                <fieldset disabled={saving}>
                    <legend>Bir romanda en çok neye önem verirsin? <small>En fazla 3</small></legend>
                    <div className="preference-options">{criteria.map(([key, label]) => <label key={key}>
                        <input type="checkbox" checked={preferences.criteria.includes(key)}
                            disabled={!preferences.criteria.includes(key) && preferences.criteria.length >= 3}
                            onChange={() => toggle('criteria', key)} />{label}
                    </label>)}</div>
                </fieldset>
                <button className="button primary-button" disabled={saving} type="submit">{saving ? 'Kaydediliyor…' : 'Tercihlerimi kaydet'}</button>
            </form>
        </div>}
        {!loading && result && <>
            <button type="button" className="recommendation-refresh" disabled={saving} onClick={() => void refreshRecommendations()}>Önerileri yenile</button>
            {result.feedback.length > 0 && <details className="recommendation-feedback-history">
                <summary>Geri bildirimlerim ({result.feedback.length})</summary>
                <p>“İlgimi çekti” romanı okuma listene ekler. Geri bildirimi geri almak kitabı listenden silmez.</p>
                <ul>{result.feedback.map(item => <li key={item.bookId}>
                    <span>{item.title} · {item.choice === 'interested' ? 'İlgimi çekti' : 'Bana uygun değil'}</span>
                    <button type="button" disabled={saving} aria-label={`${item.title} için geri bildirimi geri al`}
                        onClick={() => void giveFeedback(item.bookId, item.title, null)}>Geri bildirimi kaldır</button>
                </li>)}</ul>
            </details>}
            {result.items.length === 0 ? <p>Yeni öneri yok. <Link to="/catalog">Kataloğu keşfet</Link></p>
                : <div className="recommendation-shelves">{[
                    { key: 'perfect', title: 'Dört dörtlük eşleşmeler' },
                    { key: 'personal', title: 'Sana göre romanlar' },
                    { key: 'discovery', title: 'Başka türlerde, aynı keyif' },
                ].map(shelf => {
                    const books = result.items.filter(book => (book.shelf ?? 'personal') === shelf.key)
                    if (!books.length) return null
                    return <section key={shelf.key} className={`recommendation-shelf recommendation-shelf-${shelf.key}`} aria-labelledby={`shelf-${shelf.key}`}>
                    <header><h3 id={`shelf-${shelf.key}`}>{shelf.title}</h3></header>
                    <div className="recommendation-grid">{books.map(book => <article key={book.bookId}>
                    <Link className="recommendation-book" onClick={event => { if (preview) event.preventDefault() }} aria-disabled={preview || undefined} to={`/catalog/books/${book.bookId}`}>
                        <img src={book.coverImageUrl} alt="" loading="lazy" />
                        <h3>{book.title}</h3>
                    </Link>
                    <p className="recommendation-genres">{book.genres?.join(' · ')}</p>
                    {Object.keys(book.priorityScores ?? {}).length > 0 && <div className="recommendation-score-chips">{Object.entries(book.priorityScores).map(([key, score]) => <span key={key}>{criteria.find(([id]) => id === key)?.[1]} <strong>{score.toLocaleString('tr-TR', { maximumFractionDigits: 2 })}/10</strong></span>)}<small>{book.ratingCount} değerlendirme</small></div>}
                    {book.reasons.length > 0 && <details className="recommendation-explanation"><summary>Neden bu roman?</summary><ul>{book.reasons.map(reason => <li key={reason}>{reason}</li>)}</ul></details>}
                    {book.ratingCount < 3 && <small>Okur puanı verisi henüz sınırlı.</small>}
                    <div className="recommendation-feedback-actions" role="group" aria-label={`${book.title} için geri bildirim`}>
                        <button type="button" disabled={saving}
                            aria-pressed={result.feedback.some(f => f.bookId === book.bookId && f.choice === 'interested')}
                            onClick={() => void giveFeedback(book.bookId, book.title,
                                'interested')}>İlgimi çekti</button>
                        <button type="button" disabled={saving} onClick={() => void giveFeedback(book.bookId, book.title, 'dismissed')}>Bana uygun değil</button>
                    </div>
                </article>)}</div></section>
                })}</div>}
            <details className="discovery-footnote"><summary>Öneriler nasıl seçiliyor?</summary><p>Dört dörtlük eşleşme için en az bir tür ve üç öncelik seçmelisin. Her önceliğin okur ortalaması 9’un üzerinde ve romanda en az üç değerlendirme olmalı. Bu bir beğeni garantisi değildir.</p></details>
        </>}
    </section>
}
