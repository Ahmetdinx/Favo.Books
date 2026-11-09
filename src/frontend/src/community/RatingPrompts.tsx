import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookSummaries } from '../catalog/catalogApi'
import type { BookSummary } from '../catalog/catalogTypes'
import { cleanBookTitle } from '../catalog/bookTitle'
import BookObject from '../components/BookObject'
import './RatingPrompts.css'

export default function RatingPrompts() {
    const { session } = useAuth()
    return session ? <UserRatingPrompts key={session.userId} /> : null
}

function UserRatingPrompts() {
    const authenticatedFetch = useAuthenticatedFetch()
    const [books, setBooks] = useState<BookSummary[]>([])
    const [loading, setLoading] = useState(true)
    const [failed, setFailed] = useState(false)
    const [hidden, setHidden] = useState(false)
    const [revision, setRevision] = useState(0)

    useEffect(() => {
        const refresh = () => setRevision(value => value + 1)
        window.addEventListener('focus', refresh)
        return () => window.removeEventListener('focus', refresh)
    }, [])

    useEffect(() => {
        const controller = new AbortController()
        async function load() {
            setLoading(true)
            setFailed(false)
            try {
                const response = await authenticatedFetch('/api/community/me/rating-prompts',
                    { signal: controller.signal, cache: 'no-store' })
                if (!response.ok) throw new Error('Prompts unavailable')
                const result = await response.json() as { bookIds: string[] }
                const summaries = await getBookSummaries({ bookIds: result.bookIds }, controller.signal)
                if (!controller.signal.aborted) {
                    const byId = new Map(summaries.items.map(book => [book.bookId, book]))
                    setBooks(result.bookIds.flatMap(id => byId.has(id) ? [byId.get(id)!] : []))
                }
            } catch {
                if (!controller.signal.aborted) setFailed(true)
            } finally {
                if (!controller.signal.aborted) setLoading(false)
            }
        }
        void load()
        return () => controller.abort()
    }, [authenticatedFetch, revision])

    if (loading) return <p className="rating-prompts-status" role="status">Değerlendirilecek kitapların kontrol ediliyor…</p>
    if (failed) return <p className="rating-prompts-status" role="status">
        Değerlendirme hatırlatmaları yüklenemedi.{' '}
        <button type="button" onClick={() => setRevision(value => value + 1)}>Tekrar dene</button>
    </p>
    if (books.length === 0) return null
    if (hidden) return <button className="rating-prompts-restore" type="button" onClick={() => setHidden(false)}>
        Değerlendirilecek kitaplarımı göster
    </button>

    return <section className="rating-prompts" aria-labelledby="rating-prompts-heading">
        <header>
            <div>
                <h2 id="rating-prompts-heading">Değerlendir</h2>
            </div>
            <button type="button" className="rating-prompts-dismiss" onClick={() => setHidden(true)}>Gizle</button>
        </header>
        <ul>
            {books.map(book => <li key={book.bookId}>
                <div className="rating-prompt-book">
                    <BookObject title={book.title} coverImageUrl={book.coverImageUrl} size="xs" />
                    <h3><Link to={`/catalog/books/${book.bookId}`}>{cleanBookTitle(book.title)}</Link></h3>
                </div>
                <Link className="button secondary-button" to={`/catalog/books/${book.bookId}#book-rating`}>
                    Değerlendir
                </Link>
            </li>)}
        </ul>
    </section>
}
