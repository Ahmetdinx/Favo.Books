import { useEffect, useState } from 'react'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { useAuth } from '../auth/useAuth'
import AchievementTracks, { type AchievementTrack } from './AchievementTracks'

export default function ProfileAchievements() {
    const authFetch = useAuthenticatedFetch()
    const { session } = useAuth()
    const [items, setItems] = useState<AchievementTrack[] | null>(null)
    const [error, setError] = useState(false)
    const [version, setVersion] = useState(0)
    useEffect(() => {
        const refresh = () => setVersion(v => v + 1)
        window.addEventListener('library-changed', refresh)
        window.addEventListener('focus', refresh)
        return () => { window.removeEventListener('library-changed', refresh); window.removeEventListener('focus', refresh) }
    }, [])
    useEffect(() => {
        const controller = new AbortController()
        setItems(null); setError(false)
        if (!session?.userId) return
        authFetch('/api/readers/me/achievements', { signal: controller.signal, cache: 'no-store' })
            .then(async response => {
                if (!response.ok) throw new Error()
                const data = await response.json() as { items: AchievementTrack[] }
                if (!controller.signal.aborted) setItems(data.items)
            }).catch(() => { if (!controller.signal.aborted) setError(true) })
        return () => controller.abort()
    }, [authFetch, session?.userId, version])
    if (error) return <p role="alert" className="achievement-caption">Başarılar yüklenemedi. <button onClick={() => setVersion(v => v + 1)}>Tekrar dene</button></p>
    if (!items) return <p role="status" className="achievement-caption">Başarıların yükleniyor…</p>
    return <AchievementTracks items={items} />
}
