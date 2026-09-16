import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import './BookRecommendations.css'

const criteria = [
    ['theme', 'Konu / Fikir'], ['language', 'Anlatım ve Dil'], ['characters', 'Karakterler'],
    ['structure', 'Kurgu ve Yapı'], ['pace', 'Tempo / Sürükleyicilik'],
    ['impact', 'Duygusal / Düşünsel Etki'], ['experience', 'Genel Okuma Deneyimi'],
] as const
type Preferences = { categories: string[]; criteria: string[]; availableCategories: { slug: string; name: string }[] }
type FeedbackItem = { bookId: string; title: string; choice: 'interested' | 'dismissed' }
type Recommendations = { hasPersonalSignals: boolean; feedback: FeedbackItem[]; items: {
    bookId: string; title: string; coverImageUrl: string; reasons: string[]; ratingCount: number
}[] }

export default function BookRecommendations({ home = false }: { home?: boolean }) {
    const { session } = useAuth()
    return session ? <PersonalRecommendations key={session.userId} home={home} /> : null
}

function PersonalRecommendations({ home }: { home: boolean }) {
    const Heading = home ? 'h1' : 'h2'
    const request = useAuthenticatedFetch()
    const [preferences, setPreferences] = useState<Preferences | null>(null)
    const [result, setResult] = useState<Recommendations | null>(null)
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [retry, setRetry] = useState(0)
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
                if (!controller.signal.aborted) { setPreferences(prefs); setResult(recommendations) }
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
            const updated = await request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            if (!updated.ok) throw new Error()
            const data = await updated.json() as Recommendations
            if (!controller.signal.aborted) { setResult(data); setMessage('Tercihlerin kaydedildi, önerilerin güncellendi.') }
        } catch { if (!controller.signal.aborted) setError(saved
            ? 'Tercihlerin kaydedildi fakat öneriler yenilenemedi. Tekrar deneyebilirsin.'
            : 'Tercihlerin kaydedilemedi. Seçimlerin burada duruyor; tekrar deneyebilirsin.') }
        finally { if (!controller.signal.aborted) setSaving(false) }
    }

    async function giveFeedback(bookId: string, title: string, choice: FeedbackItem['choice'] | null) {
        if (saving) return
        const controller = new AbortController()
        saveRequest.current = controller
        setSaving(true); setError(''); setMessage('')
        let saved = false
        try {
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
                items: choice === 'dismissed' ? current.items.filter(b => b.bookId !== bookId) : current.items,
                feedback: [...current.feedback.filter(f => f.bookId !== bookId), ...(choice ? [{ bookId, title, choice }] : [])],
            } : current)
            const refreshed = await request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            if (!refreshed.ok) throw new Error()
            const data = await refreshed.json() as Recommendations
            if (!controller.signal.aborted) {
                setResult(data)
                setMessage(choice === 'dismissed' ? `«${title}» önerilerinden çıkarıldı. Geri bildirimlerim alanından geri alabilirsin.`
                    : choice === 'interested' ? `«${title}» ilgini çeken kitaplara eklendi. Önerilerin güncellendi.` : 'Geri bildirimin kaldırıldı, önerilerin güncellendi.')
            }
        } catch { if (!controller.signal.aborted) setError(saved
            ? 'Geri bildirimin kaydedildi ancak öneriler yenilenemedi. Önerileri yenileyebilirsin.'
            : 'Geri bildirim tamamlanamadı. Lütfen tekrar dene.') }
        finally { if (!controller.signal.aborted) setSaving(false) }
    }

    return <section className="personal-discovery" aria-labelledby="discovery-heading" aria-busy={loading || saving}>
        <p className="eyebrow">Sıradaki sayfan</p>
        <Heading id="discovery-heading">Şimdi ne okumalı?</Heading>
        <p className="discovery-intro">Sevdiğin kitaplardan ve seçtiğin özelliklerden yola çıkan öneriler.
            Okuduğun ve okumakta olduğun kitapları burada tekrar göstermiyoruz.</p>
        {loading && <p role="status">Kitapların arasından yeni okumalar seçiliyor…</p>}
        {error && <p role="alert">{error} {!preferences && <button type="button" onClick={() => setRetry(n => n + 1)}>Tekrar dene</button>}</p>}
        {message && <p role="status">{message}</p>}
        {!loading && preferences && <details className="reading-preferences">
            <summary>Okuma tercihlerim <span>{preferences.categories.length} tür · {preferences.criteria.length} öncelik</span></summary>
            <form onSubmit={save}>
                <p>Bir kitaba verdiğin puan, o özelliğin senin için ne kadar önemli olduğunu söylemez.
                    Burada kendi önceliklerini seçebilirsin. Seçimleri kaldırarak tercihlerini sıfırlayabilirsin.</p>
                <fieldset disabled={saving}>
                    <legend>Hangi türleri keşfetmek istersin? <small>En fazla 5</small></legend>
                    <div className="preference-options">{preferences.availableCategories.map(category => <label key={category.slug}>
                        <input type="checkbox" checked={preferences.categories.includes(category.slug)}
                            disabled={!preferences.categories.includes(category.slug) && preferences.categories.length >= 5}
                            onChange={() => toggle('categories', category.slug)} />{category.name}
                    </label>)}</div>
                </fieldset>
                <fieldset disabled={saving}>
                    <legend>Bir kitapta en çok neye önem verirsin? <small>En fazla 3</small></legend>
                    <div className="preference-options">{criteria.map(([key, label]) => <label key={key}>
                        <input type="checkbox" checked={preferences.criteria.includes(key)}
                            disabled={!preferences.criteria.includes(key) && preferences.criteria.length >= 3}
                            onChange={() => toggle('criteria', key)} />{label}
                    </label>)}</div>
                </fieldset>
                <button className="button primary-button" disabled={saving} type="submit">{saving ? 'Kaydediliyor…' : 'Tercihlerimi kaydet'}</button>
            </form>
        </details>}
        {!loading && result && <>
            <button type="button" className="recommendation-refresh" disabled={saving} onClick={() => void refreshRecommendations()}>Önerileri yenile</button>
            {result.feedback.length > 0 && <details className="recommendation-feedback-history">
                <summary>Geri bildirimlerim ({result.feedback.length})</summary>
                <p>İlgi seçimlerin benzer kitapları öne çıkarır. “Bana uygun değil” yalnızca seçtiğin kitabı gizler.
                    Bu seçimler kitap puanını veya okuma listeni değiştirmez.</p>
                <ul>{result.feedback.map(item => <li key={item.bookId}>
                    <span>{item.title} · {item.choice === 'interested' ? 'İlgimi çekti' : 'Bana uygun değil'}</span>
                    <button type="button" disabled={saving} aria-label={`${item.title} için geri bildirimi geri al`}
                        onClick={() => void giveFeedback(item.bookId, item.title, null)}>Geri al</button>
                </li>)}</ul>
            </details>}
            {!result.hasPersonalSignals && <p className="discovery-hint">İlk önerilerin keşif amaçlı.
                Okuma tercihlerini seç veya okuduğun birkaç kitabı değerlendir; seni tanımaya başlayalım.</p>}
            {result.items.length === 0 ? <p>Şimdilik önerebileceğimiz yeni bir kitap yok. <Link to="/catalog">Kataloğu keşfet</Link></p>
                : <div className="recommendation-grid">{result.items.map(book => <article key={book.bookId}>
                    <Link className="recommendation-book" to={`/catalog/books/${book.bookId}`}>
                        <img src={book.coverImageUrl} alt="" loading="lazy" />
                        <h3>{book.title}</h3>
                    </Link>
                    <ul>{book.reasons.map(reason => <li key={reason}>{reason}</li>)}</ul>
                    {book.ratingCount < 3 && <small>Okur puanı verisi henüz sınırlı.</small>}
                    <div className="recommendation-feedback-actions" role="group" aria-label={`${book.title} için geri bildirim`}>
                        <button type="button" disabled={saving}
                            aria-pressed={result.feedback.some(f => f.bookId === book.bookId && f.choice === 'interested')}
                            onClick={() => void giveFeedback(book.bookId, book.title,
                                result.feedback.some(f => f.bookId === book.bookId && f.choice === 'interested') ? null : 'interested')}>İlgimi çekti</button>
                        <button type="button" disabled={saving} onClick={() => void giveFeedback(book.bookId, book.title, 'dismissed')}>Bana uygun değil</button>
                    </div>
                </article>)}</div>}
            <p className="discovery-footnote">Tür ve yazar benzerliği bir başlangıçtır. Kriter puanlarını en az üç değerlendirme olduğunda kullanıyoruz;
                öneriler kesin bir beğeni tahmini değildir.</p>
        </>}
    </section>
}
