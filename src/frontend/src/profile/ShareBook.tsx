import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getBookRatings } from '../community/communityApi'
import { getLibraryEntryByBook } from '../reading/readingApi'
import { getReaderSettings } from './profileVisibility'
import { renderBookCard, type BookShare } from './shareCards'
import ShareSheet from './ShareSheet'
import { loadSignatureBook, profileUrl, type CardFormat, type SignatureBook } from './tasteCardImage'

/**
 * "Share" on a book page: a rating card once the reader has rated the book, otherwise a "now reading"
 * or "finished" card from their shelf. Hidden for books not on the Reading or Read shelf.
 */
export default function ShareBook({ bookId, summary }: { bookId: string; summary: string | null }) {
    const { session } = useAuth()
    const request = useAuthenticatedFetch()
    const [state, setState] = useState<{ kind: BookShare['kind']; score: number | null } | null>(null)
    const [version, setVersion] = useState(0)
    const [open, setOpen] = useState(false)
    const [book, setBook] = useState<SignatureBook | null | undefined>(undefined)
    const [viewer, setViewer] = useState<{ nickname: string | null; isPublic: boolean } | undefined>(undefined)

    useEffect(() => {
        const refresh = () => setVersion(v => v + 1)
        window.addEventListener('library-changed', refresh)
        window.addEventListener('achievements-changed', refresh)
        return () => { window.removeEventListener('library-changed', refresh); window.removeEventListener('achievements-changed', refresh) }
    }, [])

    useEffect(() => {
        if (!session) { setState(null); return }
        const controller = new AbortController()
        Promise.all([getLibraryEntryByBook(request, bookId, controller.signal), getBookRatings(bookId, request, controller.signal)])
            .then(([entry, ratings]) => {
                const score = ratings.currentUserRating?.overallScore ?? null
                setState(score != null ? { kind: 'rating', score }
                    : entry?.status === 'Reading' ? { kind: 'reading', score: null }
                    : entry?.status === 'Read' ? { kind: 'finished', score: null } : null)
            })
            .catch(() => { if (!controller.signal.aborted) setState(null) })
        return () => controller.abort()
    }, [bookId, request, session, version])

    useEffect(() => {
        if (!open) return
        let active = true
        void loadSignatureBook(bookId).then(value => { if (active) setBook(value) })
        getReaderSettings(request).then(s => { if (active) setViewer({ nickname: s.nickname, isPublic: s.isPublic }) })
            .catch(() => { if (active) setViewer({ nickname: null, isPublic: false }) })
        return () => { active = false }
    }, [open, bookId, request])

    const linkable = !!viewer?.nickname && viewer.isPublic
    const draw = useCallback((format: CardFormat) => renderBookCard({ kind: state!.kind, nickname: viewer?.nickname ?? null, book: book!, score: state!.score, summary },
        format, linkable), [state, viewer, book, summary, linkable])

    if (!state) return null
    const rating = state.kind === 'rating'
    const finished = state.kind === 'finished'
    return <>
        <button type="button" className="book-share-button" onClick={() => setOpen(true)}>{rating ? 'Puanını paylaş' : finished ? 'Okuduğunu paylaş' : 'Okuduğun kitabı paylaş'}</button>
        <ShareSheet open={open} onClose={() => setOpen(false)} title={rating ? 'Puanını paylaş' : finished ? 'Okuduğun kitabı paylaş' : 'Şu an okuduğunu paylaş'}
            alt={book ? `${book.title} ${rating ? 'puan' : finished ? 'okudum' : 'okuyorum'} kartı` : 'Kitap kartı'}
            render={book && viewer ? draw : null} fileName="favo-kitap.png"
            shareText={book ? (rating ? `«${book.title}» için puanım ${state.score?.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}/10.` : finished ? `«${book.title}» okudum.` : `Şu an «${book.title}» okuyorum.`) : ''}
            link={linkable ? profileUrl(viewer!.nickname!, 'kitap') : null}
            note={linkable ? 'Kartta profil adresin yazar.' : 'Profilin gizli olduğu için kartta yalnızca Favo adresi yazar.'} />
    </>
}
