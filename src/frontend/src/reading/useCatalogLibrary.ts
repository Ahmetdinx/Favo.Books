import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { addBookToLibrary, getLibraryEntryByBook, updateLibraryStatus, ReadingApiError } from './readingApi'
import type { LibraryEntry, LibraryEntryStatus } from './readingTypes'

type Undo = { entry: LibraryEntry; previous: LibraryEntry | null }
export function useCatalogLibrary(bookIds: string[]) {
    const { session, isInitializing } = useAuth()
    const authFetch = useAuthenticatedFetch()
    const location = useLocation()
    const navigate = useNavigate()
    const [entries, setEntries] = useState<Record<string, LibraryEntry>>({})
    const [busy, setBusy] = useState<Record<string, boolean>>({})
    const [errors, setErrors] = useState<Record<string, string>>({})
    const [undos, setUndos] = useState<Record<string, Undo | undefined>>({})
    const [loading, setLoading] = useState(true)
    const [loadError, setLoadError] = useState(false)
    const [version, setVersion] = useState(0)
    const locks = useRef(new Set<string>())
    const epoch = useRef(0)
    const ids = JSON.stringify(bookIds)
    const userId = session?.userId
    useEffect(() => {
        const controller = new AbortController()
        const generation = ++epoch.current
        locks.current.clear()
        setEntries({}); setUndos({}); setErrors({}); setBusy({}); setLoadError(false)
        if (!userId) { setLoading(false); return }
        const wanted = JSON.parse(ids) as string[]
        if (!wanted.length) { setLoading(false); return }
        setLoading(true)
        authFetch('/api/reading/library/lookup', { method: 'POST', signal: controller.signal,
            headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ bookIds: wanted }) })
            .then(async response => {
                if (!response.ok) throw new Error()
                const data = await response.json() as { items: LibraryEntry[] }
                if (!controller.signal.aborted) setEntries(Object.fromEntries(data.items.map(e => [e.bookId, e])))
            }).catch(() => { if (!controller.signal.aborted) setLoadError(true) })
            .finally(() => { if (!controller.signal.aborted) setLoading(false) })
        return () => { controller.abort(); epoch.current = generation + 1 }
    }, [ids, userId, authFetch, version])

    function login(bookId: string, status: LibraryEntryStatus) {
        const params = new URLSearchParams(location.search)
        params.set('addBook', bookId); params.set('readingStatus', status)
        navigate('/login', { state: { from: location.pathname + '?' + params } })
    }
    async function refreshBook(bookId: string, started: number) {
        const latest = await getLibraryEntryByBook(authFetch, bookId)
        if (started !== epoch.current) return
        setEntries(current => {
            const next = { ...current }
            if (latest) next[bookId] = latest
            else delete next[bookId]
            return next
        })
    }
    async function change(bookId: string, status: LibraryEntryStatus) {
        if (!session) { login(bookId, status); return }
        if (loading || loadError || locks.current.has(bookId)) return
        const previous = entries[bookId] ?? null
        if (previous?.status === status) return
        locks.current.add(bookId)
        const started = epoch.current
        setBusy(current => ({ ...current, [bookId]: true }))
        setErrors(current => ({ ...current, [bookId]: '' }))
        try {
            let entry: LibraryEntry
            if (previous) {
                const updated = await updateLibraryStatus(authFetch, previous.libraryEntryId, status)
                entry = { ...previous, ...updated }
            } else {
                const added = await addBookToLibrary(authFetch, { bookId, status })
                entry = { ...added, bookId, status, createdAtUtc: added.updatedAtUtc }
            }
            if (started !== epoch.current) return
            setEntries(current => ({ ...current, [bookId]: entry }))
            setUndos(current => ({ ...current, [bookId]: { entry, previous } }))
        } catch (e) {
            if (started !== epoch.current) return
            if (e instanceof ReadingApiError && e.status === 401) { login(bookId, status); return }
            // A duplicate or uncertain network response needs a fresh state before retry.
            try { await refreshBook(bookId, started) } catch { /* Keep the failed action visible. */ }
            if (started !== epoch.current) return
            setUndos(current => ({ ...current, [bookId]: undefined }))
            setErrors(current => ({ ...current, [bookId]: e instanceof ReadingApiError && e.status === 409
                ? 'Kitap zaten eklenmiş; son durumu kontrol et.' : 'İşlem tamamlanamadı. Tekrar deneyebilirsin.' }))
        } finally {
            if (started === epoch.current) {
                locks.current.delete(bookId)
                setBusy(current => ({ ...current, [bookId]: false }))
            }
        }
    }
    async function undo(bookId: string) {
        const last = undos[bookId]
        if (!last || locks.current.has(bookId)) return
        locks.current.add(bookId)
        const started = epoch.current
        setBusy(current => ({ ...current, [bookId]: true }))
        setErrors(current => ({ ...current, [bookId]: '' }))
        try {
            const response = await authFetch('/api/reading/library/' + last.entry.libraryEntryId + '/undo', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ expectedUpdatedAtUtc: last.entry.updatedAtUtc, previousStatus: last.previous?.status ?? null }),
            })
            if (!response.ok) throw new ReadingApiError('Undo failed', response.status)
            window.dispatchEvent(new Event('library-changed'))
            if (started !== epoch.current) return
            setUndos(current => ({ ...current, [bookId]: undefined }))
            // Immediately restore the visible state; reload the server timestamp afterwards.
            setEntries(current => {
                const next = { ...current }
                if (last.previous) next[bookId] = last.previous
                else delete next[bookId]
                return next
            })
            await refreshBook(bookId, started)
        } catch (e) {
            if (started !== epoch.current) return
            try { await refreshBook(bookId, started) } catch { /* A retry remains available. */ }
            if (started !== epoch.current) return
            if (e instanceof ReadingApiError && e.status === 409)
                setUndos(current => ({ ...current, [bookId]: undefined }))
            setErrors(current => ({ ...current, [bookId]: e instanceof ReadingApiError && e.status === 409
                ? 'Kayıt başka bir işlemle değişmiş; geri alınmadı.' : 'Geri alma tamamlanamadı. Tekrar dene.' }))
        } finally {
            if (started === epoch.current) {
                locks.current.delete(bookId)
                setBusy(current => ({ ...current, [bookId]: false }))
            }
        }
    }
    return { entries, busy, errors, undos, loading: loading || isInitializing, loadError, change, undo,
        retry: () => setVersion(v => v + 1), signedIn: !!session }
}
export type CatalogLibrary = ReturnType<typeof useCatalogLibrary>
