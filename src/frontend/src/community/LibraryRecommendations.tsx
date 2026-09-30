import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getLibraryEntryByBook, updateLibraryStatus } from '../reading/readingApi'

type Book = { bookId: string; title: string; coverImageUrl: string; reasons: string[] }

export default function LibraryRecommendations() {
    const { session } = useAuth()
    return session ? <LibraryShelf key={session.userId} /> : null
}

function LibraryShelf() {
    const request = useAuthenticatedFetch()
    const [books, setBooks] = useState<Book[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [busy, setBusy] = useState(false)
    const pending = useRef<AbortController | null>(null)
    useEffect(() => () => pending.current?.abort(), [])
    const [version, setVersion] = useState(0)
    useEffect(() => {
        const refresh = () => setVersion(v => v + 1)
        window.addEventListener('library-changed', refresh)
        window.addEventListener('achievements-changed', refresh)
        window.addEventListener('reading-preferences-changed', refresh)
        window.addEventListener('focus', refresh)
        return () => {
            window.removeEventListener('library-changed', refresh)
            window.removeEventListener('achievements-changed', refresh)
            window.removeEventListener('reading-preferences-changed', refresh)
            window.removeEventListener('focus', refresh)
        }
    }, [])
    useEffect(() => {
        const controller = new AbortController()
        setLoading(true)
        setError('')
        request('/api/community/me/recommendations/library', { signal: controller.signal, cache: 'no-store' })
            .then(async response => {
                if (!response.ok) throw Error()
                const data = await response.json() as { items: Book[] }
                if (!controller.signal.aborted) setBooks(data.items)
            }).catch(() => { if (!controller.signal.aborted) setError('Listendeki öneriler yüklenemedi.') })
            .finally(() => { if (!controller.signal.aborted) setLoading(false) })
        return () => controller.abort()
    }, [request, version])

    async function start(book: Book) {
        if (busy || pending.current) return
        const controller = new AbortController()
        pending.current = controller
        setBusy(true); setError(''); setMessage('')
        try {
            const entry = await getLibraryEntryByBook(request, book.bookId, controller.signal)
            if (!entry || entry.status !== 'WantToRead') {
                setMessage('Bu romanın okuma durumu değişmiş. Listen yenilendi.')
                setVersion(v => v + 1)
                return
            }
            await updateLibraryStatus(request, entry.libraryEntryId, 'Reading', controller.signal)
            if (controller.signal.aborted) return
            setBooks(current => current.filter(item => item.bookId !== book.bookId))
            setMessage(`«${book.title}» artık “Okuyorum” listende. Keyifli okumalar!`)
        } catch { if (!controller.signal.aborted) setError('Okuma durumu güncellenemedi. Tekrar deneyebilirsin.') }
        finally { pending.current = null; if (!controller.signal.aborted) setBusy(false) }
    }

    return <section className="personal-discovery library-recommendations" aria-labelledby="library-next-title" aria-busy={loading || busy}>
        <h2 id="library-next-title">Okuma listemden</h2>
        {message && <p role="status">{message}</p>}
        {error && <p role="alert">{error} <button type="button" onClick={() => setVersion(v => v + 1)}>Tekrar dene</button></p>}
        {loading && <p role="status">Listen gözden geçiriliyor…</p>}
        {!loading && !error && !books.length && <p>Yeni öneri yok. <Link to="/catalog">Okuma listene roman ekle</Link></p>}
        <div className="recommendation-grid">{books.map(book => <article key={book.bookId}>
            <Link className="recommendation-book" to={`/catalog/books/${book.bookId}`}>
                <img src={book.coverImageUrl} alt="" loading="lazy" /><h3>{book.title}</h3>
            </Link>
            {book.reasons.length > 0 && <details className="recommendation-explanation"><summary>Neden bu roman?</summary><ul>{book.reasons.map(reason => <li key={reason}>{reason}</li>)}</ul></details>}
            <div className="recommendation-feedback-actions"><button type="button" disabled={busy || loading} onClick={() => void start(book)}>Okumaya başla</button></div>
        </article>)}</div>
    </section>
}
