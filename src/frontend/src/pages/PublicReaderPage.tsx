import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { getBookSummaries } from '../catalog/catalogApi'
import { catalogCoverUrl } from '../catalog/coverImage'
import type { BookSummary } from '../catalog/catalogTypes'
import '../profile/Readers.css'
import AchievementTracks, { type AchievementTrack } from '../profile/AchievementTracks'
import ReaderAvatar from '../profile/ReaderAvatar'
import { FollowReader } from '../profile/ReaderSocial'

const criteria = { theme: 'Konu / Fikir', language: 'Anlatım ve Dil', characters: 'Karakterler', structure: 'Kurgu ve Yapı',
    pace: 'Tempo', impact: 'Duygusal / Düşünsel Etki', experience: 'Okuma Deneyimi' }
type Rating = { bookId: string; overallScore: number | null } & Record<keyof typeof criteria, number | null>
type Reader = { nickname: string; avatarId: string; achievements: AchievementTrack[]; library: { items: { bookId: string; status: string }[]; totalCount: number };
    showcase: { bedsideBookId: string | null; influentialBooks: { bookId: string; position: number }[] }; ratings: Rating[] }
const statuses: Record<string, string> = { WantToRead: 'Okumak istiyor', Reading: 'Okuyor', Read: 'Okudu', OnHold: 'Ara verdi', Dropped: 'Bıraktı' }

function Cover({ book }: { book: BookSummary }) {
    const [failed, setFailed] = useState(false)
    return book.coverImageUrl && !failed
        ? <img src={catalogCoverUrl(book.coverImageUrl)} alt={book.title} loading="lazy" onError={() => setFailed(true)} />
        : <span className="reader-cover-empty" aria-label="Kapak bulunamadı">{book.title[0]}</span>
}

export default function PublicReaderPage() {
    const { nickname = '' } = useParams()
    const [params, setParams] = useSearchParams()
    const page = Math.max(1, Number(params.get('page')) || 1)
    const [data, setData] = useState<Reader | null>(null)
    const [books, setBooks] = useState<BookSummary[]>([])
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(true)
    const [version, setVersion] = useState(0)
    useEffect(() => {
        const controller = new AbortController()
        setData(null); setBooks([]); setBusy(true); setError('')
        async function load() {
            try {
                const response = await fetch('/api/readers/' + encodeURIComponent(nickname) + '?page=' + page, { signal: controller.signal, cache: 'no-store' })
                if (response.status === 404) throw new Error('Profil bulunamadı veya herkese açık değil.')
                if (!response.ok) throw new Error('Profil yüklenemedi.')
                const result = await response.json() as Reader
                const ids = [...new Set([...result.library.items.map(b => b.bookId), ...result.showcase.influentialBooks.map(b => b.bookId),
                    ...(result.showcase.bedsideBookId ? [result.showcase.bedsideBookId] : [])])]
                const summaries = await getBookSummaries({ bookIds: ids }, controller.signal)
                if (!controller.signal.aborted) { setData(result); setBooks(summaries.items) }
            } catch (e) { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Profil yüklenemedi.') }
            finally { if (!controller.signal.aborted) setBusy(false) }
        }
        void load()
        // Revalidate visibility when returning to a previously opened tab.
        const refresh = () => { if (document.visibilityState === 'visible') setVersion(v => v + 1) }
        document.addEventListener('visibilitychange', refresh)
        return () => { controller.abort(); document.removeEventListener('visibilitychange', refresh) }
    }, [nickname, page, version])
    const byId = new Map(books.map(b => [b.bookId, b]))
    function card(id: string, status?: string) {
        const book = byId.get(id)
        if (!book) return <article className="reader-book" key={id}><p>Katalogda bulunamayan kitap</p></article>
        const rating = data?.ratings.find(r => r.bookId === id)
        return <article className="reader-book" key={id}>
            <Link to={'/catalog/books/' + id}><Cover key={book.coverImageUrl} book={book} /><h3>{book.title}</h3></Link>
            <p>{book.authors.map(a => a.name).join(', ')}</p>
            {status && <small>{statuses[status] ?? status}</small>}
            {rating?.overallScore != null && <details><summary>{rating.overallScore.toFixed(1)} / 10 · Puanlar</summary>
                <dl>{Object.entries(criteria).map(([key, label]) => <div key={key}><dt>{label}</dt><dd>{rating[key as keyof typeof criteria] ?? '—'}</dd></div>)}</dl>
            </details>}
        </article>
    }
    return <section className="readers-page">
        <Link to="/">← Ana Sayfa</Link>
        {busy ? <p role="status">Kitaplık yükleniyor…</p> : error ? <div role="alert"><p>{error}</p><button onClick={() => setVersion(v => v + 1)}>Tekrar dene</button></div> : data && <>
            <header className="public-reader-header"><ReaderAvatar avatarId={data.avatarId} achievements={data.achievements} size="large" />
                <div><h1>@{data.nickname}</h1><p>{data.library.totalCount} roman · Açık kitaplık</p></div></header>
            <FollowReader key={data.nickname} nickname={data.nickname} />
            {(data.showcase.bedsideBookId || data.showcase.influentialBooks.length > 0) && <div className="reader-showcase">
                {data.showcase.bedsideBookId && <section><h2>Başucu kitabı</h2>{card(data.showcase.bedsideBookId)}</section>}
                {data.showcase.influentialBooks.length > 0 && <section><h2>İz bırakanlar</h2><div className="reader-rail">{data.showcase.influentialBooks.map(b => card(b.bookId))}</div></section>}
            </div>}
            <AchievementTracks items={data.achievements} publicView />
            <h2>Kitaplık</h2>
            {data.library.totalCount === 0 ? <p>Henüz kitap eklenmemiş.</p> : <div className="reader-books">{data.library.items.map(b => card(b.bookId, b.status))}</div>}
            {data.library.totalCount > 20 && <div className="reader-pagination">
                <button disabled={page === 1} onClick={() => setParams({ page: String(page - 1) })}>Önceki</button>
                <span>{page} / {Math.ceil(data.library.totalCount / 20)}</span>
                <button disabled={page * 20 >= data.library.totalCount} onClick={() => setParams({ page: String(page + 1) })}>Sonraki</button>
            </div>}
        </>}
    </section>
}
