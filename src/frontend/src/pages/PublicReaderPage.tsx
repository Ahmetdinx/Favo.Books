import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { getBookSummaries } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'
import BookObject from '../components/BookObject'
import './PublicReader.css'
import type { BookSummary } from '../catalog/catalogTypes'
import '../profile/Readers.css'
import AchievementTracks, { type AchievementTrack } from '../profile/AchievementTracks'
import ReaderAvatar from '../profile/ReaderAvatar'
import { FollowReader } from '../profile/ReaderSocial'
import TasteCard from '../profile/TasteCard'
import CompatibilityCard from '../profile/CompatibilityCard'
import ReaderInvite from '../profile/ReaderInvite'
import { clearInvite, pendingInvite } from './onboardingState'
import { useTasteProfile } from '../profile/useTasteProfile'
import { useAuth } from '../auth/useAuth'

const criteria = { theme: 'Konu / Fikir', language: 'Anlatım ve Dil', characters: 'Karakterler', structure: 'Kurgu ve Yapı',
    pace: 'Tempo', impact: 'Duygusal / Düşünsel Etki', experience: 'Okuma Deneyimi' }
type Rating = { bookId: string; overallScore: number | null } & Record<keyof typeof criteria, number | null>
type Reader = { nickname: string; avatarId: string; achievements: AchievementTrack[]; library: { items: { bookId: string; status: string }[]; totalCount: number };
    showcase: { bedsideBookId: string | null; influentialBooks: { bookId: string; position: number }[] }; ratings: Rating[] }
const statuses: Record<string, string> = { WantToRead: 'Okuma listesi', Reading: 'Okuyor', Read: 'Okudu', OnHold: 'Ara verdi', Dropped: 'Bıraktı' }

export default function PublicReaderPage() {
    const { nickname = '' } = useParams()
    const [params, setParams] = useSearchParams()
    const page = Math.max(1, Number(params.get('page')) || 1)
    const [data, setData] = useState<Reader | null>(null)
    const [books, setBooks] = useState<BookSummary[]>([])
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(true)
    const [version, setVersion] = useState(0)
    const { session, isInitializing } = useAuth()
    const { profile: taste } = useTasteProfile(false, nickname)
    // A signed-in visit to the inviting profile completes the invitation.
    useEffect(() => { if (session && pendingInvite()?.toLowerCase() === nickname.toLowerCase()) clearInvite() }, [session, nickname])
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
    const ratings = new Map(data?.ratings.map(r => [r.bookId, r]))
    const titleOf = (book: BookSummary) => cleanBookTitle(book.title)
    const authorsOf = (book: BookSummary) => book.authors.map(a => a.name).join(', ')
    const bedside = data?.showcase.bedsideBookId ? byId.get(data.showcase.bedsideBookId) : undefined
    const influential = [...(data?.showcase.influentialBooks ?? [])].sort((x, y) => x.position - y.position)
        .flatMap(b => { const book = byId.get(b.bookId); return book ? [{ book, position: b.position }] : [] })
    const totalPages = data ? Math.ceil(data.library.totalCount / 20) : 1

    function scoreLabel(rating: Rating) {
        return Object.entries(criteria).map(([key, label]) => `${label}: ${rating[key as keyof typeof criteria] ?? '—'}`).join('\n')
    }

    if (busy) return <p className="public-reader-state" role="status">Kitaplık yükleniyor…</p>
    if (error) return <div className="public-reader-state" role="alert"><p>{error}</p>
        <button className="button secondary-button" type="button" onClick={() => setVersion(v => v + 1)}>Tekrar dene</button></div>
    if (!data) return null

    return <section className="public-reader-page">
        <header className="public-reader-header">
            <ReaderAvatar avatarId={data.avatarId} achievements={data.achievements} size="large" />
            <div className="public-reader-title">
                <h1>@{data.nickname}</h1>
                <p>{data.library.totalCount} roman</p>
            </div>
            <FollowReader key={data.nickname} nickname={data.nickname} />
        </header>

        {!session && !isInitializing && <ReaderInvite nickname={data.nickname} title={taste?.ready ? taste.title : null} />}
        <TasteCard key={data.nickname} own={false} nickname={data.nickname} />
        <CompatibilityCard key={'match-' + data.nickname} nickname={data.nickname} />

        {(bedside || influential.length > 0) && <div className="reader-spread">
            {bedside && <section className="reader-spread-page reader-bedside" aria-labelledby="reader-bedside-heading">
                <Link to={'/catalog/books/' + bedside.bookId} aria-label={titleOf(bedside)}>
                    <BookObject title={bedside.title} author={authorsOf(bedside)} coverImageUrl={bedside.coverImageUrl} priority />
                </Link>
                <div>
                    <h2 id="reader-bedside-heading">Başucu kitabı</h2>
                    <h3><Link to={'/catalog/books/' + bedside.bookId}>{titleOf(bedside)}</Link></h3>
                    <p>{authorsOf(bedside)}</p>
                </div>
            </section>}
            {influential.length > 0 && <section className="reader-spread-page reader-influential" aria-labelledby="reader-influential-heading">
                <h2 id="reader-influential-heading">İz bırakanlar</h2>
                <ol>{influential.map(({ book, position }) => <li key={position}>
                    <Link to={'/catalog/books/' + book.bookId} aria-label={`${position}. ${titleOf(book)}`} title={titleOf(book)}>
                        <BookObject title={book.title} author={authorsOf(book)} coverImageUrl={book.coverImageUrl} size="sm" />
                    </Link>
                    <span aria-hidden="true">{position}</span>
                </li>)}</ol>
            </section>}
        </div>}

        <section className="bookcase-section" aria-labelledby="public-bookcase-heading">
            <h2 id="public-bookcase-heading" className="public-reader-heading">Kitaplık</h2>
            {data.library.totalCount === 0 ? <div className="bookcase-state"><h3>Raflar henüz boş</h3></div>
                : <div className="bookcase">{data.library.items.map(item => {
                    const book = byId.get(item.bookId)
                    const rating = ratings.get(item.bookId)
                    const title = book ? titleOf(book) : 'Katalog kaydı bulunamadı'
                    const authors = book ? authorsOf(book) : ''
                    const volume = <BookObject title={book?.title ?? title} author={authors} coverImageUrl={book?.coverImageUrl} />
                    return <article className="bookcase-slot" key={item.bookId}>
                        <div className="bookcase-book">
                            {book ? <Link to={'/catalog/books/' + book.bookId} aria-label={title}>{volume}</Link> : volume}
                            <span className={`bookcase-ribbon status-${item.status.toLowerCase()}`}>{statuses[item.status] ?? item.status}</span>
                            {rating?.overallScore != null && <span className="bookcase-score" title={scoreLabel(rating)}
                                aria-label={`${rating.overallScore.toFixed(1)} / 10. ${scoreLabel(rating).replaceAll('\n', ', ')}`}>
                                {rating.overallScore.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}
                            </span>}
                        </div>
                        <div className="bookcase-label">
                            <h3 title={title}>{book ? <Link to={'/catalog/books/' + book.bookId}>{title}</Link> : title}</h3>
                            <p title={authors}>{authors}</p>
                        </div>
                    </article>
                })}</div>}
            {totalPages > 1 && <nav className="profile-pagination" aria-label="Kitaplık sayfaları">
                <button className="button secondary-button" type="button" disabled={page === 1} onClick={() => setParams({ page: String(page - 1) })}>Önceki</button>
                <span>{page} / {totalPages}</span>
                <button className="button secondary-button" type="button" disabled={page >= totalPages} onClick={() => setParams({ page: String(page + 1) })}>Sonraki</button>
            </nav>}
        </section>

        <AchievementTracks items={data.achievements} publicView />
    </section>
}
