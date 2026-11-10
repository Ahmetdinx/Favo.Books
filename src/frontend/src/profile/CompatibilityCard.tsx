import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookSummaries } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'
import type { BookSummary } from '../catalog/catalogTypes'
import BookObject from '../components/BookObject'
import './CompatibilityCard.css'

type Compatibility = {
    ready: boolean
    percent: number | null
    label: string | null
    sharedGenres: string[]
    commonBooks: number
    comparedRatings: number
    suggestions: { bookId: string; score: number | null; influential: boolean }[]
}

/** Signed-in reader's taste match with a public reader, plus up to three of their loved novels not yet met. */
export default function CompatibilityCard({ nickname }: { nickname: string }) {
    const { session } = useAuth()
    const request = useAuthenticatedFetch()
    const [data, setData] = useState<Compatibility | null>(null)
    const [books, setBooks] = useState<BookSummary[]>([])

    useEffect(() => {
        if (!session) return
        const controller = new AbortController()
        setData(null); setBooks([])
        void (async () => {
            try {
                const response = await request('/api/readers/' + encodeURIComponent(nickname) + '/compatibility', { signal: controller.signal, cache: 'no-store' })
                if (!response.ok) return
                const result = await response.json() as Compatibility
                const summaries = result.suggestions.length
                    ? (await getBookSummaries({ bookIds: result.suggestions.map(s => s.bookId) }, controller.signal)).items : []
                if (!controller.signal.aborted) { setData(result); setBooks(summaries) }
            } catch { /* the profile works without the match */ }
        })()
        return () => controller.abort()
    }, [nickname, request, session])

    if (!session || !data) return null
    if (!data.ready) return <section className="match-card match-card-pending" aria-label="Okur uyumu">
        <p>Uyumunuzu görmek için ikinizin de en az 5 okunmuş romanı ya da 3 değerlendirmesi olmalı.</p>
    </section>

    const byId = new Map(books.map(b => [b.bookId, b]))
    const suggestions = data.suggestions.flatMap(s => { const book = byId.get(s.bookId); return book ? [{ ...s, book }] : [] })
    const percent = data.percent ?? 0
    const details = [
        data.commonBooks > 0 && `${data.commonBooks} ortak roman`,
        data.comparedRatings > 0 && `${data.comparedRatings} ortak değerlendirme`,
    ].filter(Boolean).join(' · ')

    return <section className="match-card" aria-labelledby="match-card-heading">
        <div className="match-summary">
            <div className="match-dial" style={{ '--match': percent } as React.CSSProperties} aria-hidden="true">
                <span>%{percent}</span>
            </div>
            <div>
                <p className="match-kicker">{data.label}</p>
                <h2 id="match-card-heading">@{nickname} ile <strong>%{percent}</strong> uyumlusunuz</h2>
                {data.sharedGenres.length > 0 && <p className="match-genres">Ortak rafınız: {data.sharedGenres.join(', ')}</p>}
                {details && <p className="match-details">{details}</p>}
            </div>
        </div>
        {suggestions.length > 0 && <div className="match-suggestions">
            <h3>Onun sevdiği, senin henüz okumadığın</h3>
            <ul>{suggestions.map(({ book, score, influential }) => {
                const title = cleanBookTitle(book.title)
                const authors = book.authors.map(a => a.name).join(', ')
                return <li key={book.bookId}>
                    <Link to={'/catalog/books/' + book.bookId} aria-label={title}>
                        <BookObject title={book.title} author={authors} coverImageUrl={book.coverImageUrl} size="sm" />
                    </Link>
                    <div>
                        <Link to={'/catalog/books/' + book.bookId} className="match-book-title" title={title}>{title}</Link>
                        <p title={authors}>{authors}</p>
                        <p className="match-why">{influential ? 'İz bırakanlarından' : ''}{influential && score != null ? ' · ' : ''}
                            {score != null ? `${score.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} verdi` : ''}</p>
                    </div>
                </li>
            })}</ul>
        </div>}
    </section>
}
