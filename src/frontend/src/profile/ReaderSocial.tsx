import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookSummaries } from '../catalog/catalogApi'
import { catalogCoverUrl } from '../catalog/coverImage'
import type { BookSummary } from '../catalog/catalogTypes'
import ReaderAvatar from './ReaderAvatar'
import './ReaderSocial.css'

type Network = { followerCount: number; followingCount: number; isFollowing: boolean; isSelf: boolean }
type Reader = { nickname: string; avatarId: string }
type Connections = { items: Reader[]; hasMore: boolean }
type Activity = Reader & { bookId: string; updatedAtUtc: string; overallScore?: number }
type Feed = { reading: Activity[]; reviews: Activity[] }

function useSocial<T>(url: string | null) {
    const request = useAuthenticatedFetch()
    const { session } = useAuth()
    const userId = session?.userId
    const [data, setData] = useState<T | null>(null)
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(false)
    const [revision, setRevision] = useState(0)
    const refresh = useCallback(() => setRevision(n => n + 1), [])
    useEffect(() => {
        const visible = () => { if (document.visibilityState === 'visible') refresh() }
        document.addEventListener('visibilitychange', visible)
        window.addEventListener('reader-network-changed', refresh)
        window.addEventListener('reader-settings-changed', refresh)
        return () => {
            document.removeEventListener('visibilitychange', visible)
            window.removeEventListener('reader-network-changed', refresh)
            window.removeEventListener('reader-settings-changed', refresh)
        }
    }, [refresh])
    useEffect(() => {
        const controller = new AbortController()
        setData(null); setError(''); setBusy(!!url && !!userId)
        if (url && userId) void request(url, { signal: controller.signal, cache: 'no-store' }).then(async response => {
            if (!response.ok) throw new Error(response.status === 404 ? 'Profil artık herkese açık değil.' : 'Okur ağı yüklenemedi.')
            const result = await response.json() as T
            if (!controller.signal.aborted) setData(result)
        }).catch(e => { if (!controller.signal.aborted) setError(e.message) })
            .finally(() => { if (!controller.signal.aborted) setBusy(false) })
        return () => controller.abort()
    }, [url, userId, request, revision]) // identity changes clear the previous reader's results
    return { data, error, busy, refresh }
}

function LoadState({ busy, error, retry }: { busy: boolean; error: string; retry: () => void }) {
    return busy ? <p className="social-state" role="status">Okur ağı yükleniyor…</p>
        : error ? <div className="social-state" role="alert">{error} <button onClick={retry}>Tekrar dene</button></div> : null
}

export function FollowReader({ nickname }: { nickname: string }) {
    const { session } = useAuth()
    const request = useAuthenticatedFetch()
    const { data, busy, error, refresh } = useSocial<Network>('/api/readers/' + encodeURIComponent(nickname) + '/network')
    const [saving, setSaving] = useState(false)
    const [message, setMessage] = useState('')
    const pending = useRef<AbortController | null>(null)
    useEffect(() => () => pending.current?.abort(), [])
    async function toggle() {
        if (!data || pending.current) return
        const controller = new AbortController()
        pending.current = controller; setSaving(true); setMessage('')
        try {
            const response = await request('/api/readers/' + encodeURIComponent(nickname) + '/follow', {
                method: data.isFollowing ? 'DELETE' : 'PUT', signal: controller.signal,
            })
            if (!response.ok) {
                const body = await response.json().catch(() => ({}))
                throw new Error(body.message ?? 'Takip işlemi tamamlanamadı.')
            }
            if (!controller.signal.aborted) {
                setMessage(data.isFollowing ? 'Takipten çıktın.' : 'Takip ediyorsun.')
                window.dispatchEvent(new Event('reader-network-changed'))
            }
        } catch (e) { if (!controller.signal.aborted) setMessage(e instanceof Error ? e.message : 'İşlem tamamlanamadı.') }
        finally { if (!controller.signal.aborted) setSaving(false); pending.current = null }
    }
    if (!session) return <Link className="social-button" to="/login">Takip etmek için giriş yap</Link>
    return <div className="reader-follow">
        <LoadState busy={busy} error={error} retry={refresh} />
        {data && <><div className="social-counts"><span><strong>{data.followerCount}</strong> takipçi</span><span><strong>{data.followingCount}</strong> takip</span></div>
            {data.isSelf ? <Link className="social-button" to="/profile">Profilim</Link> : <button className="social-button" disabled={saving || busy}
                aria-pressed={data.isFollowing} onClick={() => void toggle()}>{saving ? 'Kaydediliyor…' : data.isFollowing ? 'Takipten çık' : '+ Takip et'}</button>}</>}
        {message && <p className="social-state" role="status">{message}</p>}
    </div>
}

export function ReaderConnections() {
    const [kind, setKind] = useState<'followers' | 'following' | null>(null)
    const [page, setPage] = useState(1)
    const counts = useSocial<Network>('/api/readers/me/network')
    const list = useSocial<Connections>(kind ? `/api/readers/me/connections?kind=${kind}&page=${page}` : null)
    function select(next: 'followers' | 'following') { setKind(kind === next ? null : next); setPage(1) }
    return <section className="reader-connections" aria-label="Takipçiler ve takip edilenler">
        <div className="connections-heading"><h2>Okur çevrem</h2><div className="social-counts">
            <button aria-expanded={kind === 'followers'} aria-controls="reader-connection-list" onClick={() => select('followers')}><strong>{counts.data?.followerCount ?? '—'}</strong> takipçi</button>
            <button aria-expanded={kind === 'following'} aria-controls="reader-connection-list" onClick={() => select('following')}><strong>{counts.data?.followingCount ?? '—'}</strong> takip</button>
        </div></div>
        <LoadState busy={counts.busy} error={counts.error} retry={counts.refresh} />
        {kind && <div id="reader-connection-list"><h3>{kind === 'followers' ? 'Takipçilerim' : 'Takip ettiklerim'}</h3>
            <LoadState busy={list.busy} error={list.error} retry={list.refresh} />
            {list.data && <>{!list.data.items.length ? <p className="social-state">Henüz kimse yok.</p> : <ul className="connection-list">{list.data.items.map(reader => <li key={reader.nickname}>
                <Link to={'/okur/' + reader.nickname}><ReaderAvatar avatarId={reader.avatarId} size="small" /><span>@{reader.nickname}</span><span aria-hidden="true">↗</span></Link>
            </li>)}</ul>}
            {(page > 1 || list.data.hasMore) && <nav className="social-pagination" aria-label="Okur listesi sayfaları"><button disabled={page === 1} onClick={() => setPage(p => p - 1)}>Önceki</button><span>{page}</span><button disabled={!list.data.hasMore} onClick={() => setPage(p => p + 1)}>Sonraki</button></nav>}</>}
        </div>}
    </section>
}

function ActivityCover({ book }: { book: BookSummary | undefined }) {
    const [failed, setFailed] = useState(false)
    return book?.coverImageUrl && !failed ? <img className="social-cover" src={catalogCoverUrl(book.coverImageUrl)} alt="" loading="lazy" onError={() => setFailed(true)} />
        : <span className="social-cover social-cover-empty" aria-hidden="true">▤</span>
}

export default function ReaderActivity() {
    const feed = useSocial<Feed>('/api/readers/me/activity')
    const [books, setBooks] = useState<BookSummary[]>([])
    const [catalogError, setCatalogError] = useState('')
    useEffect(() => {
        const controller = new AbortController()
        setBooks([]); setCatalogError('')
        const ids = [...new Set([...(feed.data?.reading ?? []), ...(feed.data?.reviews ?? [])].map(item => item.bookId))]
        if (ids.length) void getBookSummaries({ bookIds: ids }, controller.signal).then(result => {
            if (!controller.signal.aborted) setBooks(result.items)
        }).catch(() => { if (!controller.signal.aborted) setCatalogError('Kitap bilgileri yüklenemedi.') })
        return () => controller.abort()
    }, [feed.data])
    const byId = new Map(books.map(book => [book.bookId, book]))
    return <section className="reader-activity" aria-label="Okur çevrenden">
        <LoadState busy={feed.busy} error={feed.error || catalogError} retry={feed.refresh} />
        <div className="social-columns">{(['reading', 'reviews'] as const).map(kind => <section className={'social-panel social-panel-' + kind} key={kind}>
            <header><span className="social-panel-mark" aria-hidden="true">{kind === 'reading' ? '▤' : '✧'}</span><h2>{kind === 'reading' ? 'Takip ettiklerin okuyor' : 'Takipçilerin değerlendirdi'}</h2></header>
            {feed.data && !feed.data[kind].length && <div className="social-empty"><span aria-hidden="true">{kind === 'reading' ? '↗' : '☆'}</span><p>{kind === 'reading' ? 'Okur çevrenin sıradaki sayfaları burada.' : 'Yeni değerlendirmeler burada buluşacak.'}</p><Link to="/profile">Okur çevrem</Link></div>}
            <ul>{(feed.data?.[kind] ?? []).map(item => { const book = byId.get(item.bookId); return <li key={item.nickname + item.bookId}>
                <Link className="social-book-cover" to={'/catalog/books/' + item.bookId}><ActivityCover key={book?.coverImageUrl ?? item.bookId} book={book} /><span className="social-sr-only">{book?.title ?? 'Kitap detayları'}</span></Link>
                <div className="social-activity-copy"><Link className="social-reader" to={'/okur/' + item.nickname}><ReaderAvatar avatarId={item.avatarId} size="small" /><span>@{item.nickname}</span></Link>
                    <Link className="social-book-title" to={'/catalog/books/' + item.bookId}>{book?.title ?? 'Kitap detayları'}</Link>
                    <div className="social-meta">{kind === 'reviews' ? <strong>★ {item.overallScore?.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}<small> / 10</small></strong> : <span>Şu an okuyor</span>}
                        <time dateTime={item.updatedAtUtc}>{new Date(item.updatedAtUtc).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })}</time></div>
                </div>
            </li> })}</ul>
        </section>)}</div>
    </section>
}
