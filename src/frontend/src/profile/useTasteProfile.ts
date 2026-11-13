import { useEffect, useState } from 'react'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import type { TasteProfile } from './tasteCardImage'

/** Loads the taste profile; own profiles refresh when the library or settings change. */
export function useTasteProfile(own: boolean, nickname?: string | null) {
    const request = useAuthenticatedFetch()
    const [profile, setProfile] = useState<TasteProfile | null>(null)
    const [failed, setFailed] = useState(false)
    const [version, setVersion] = useState(0)
    const url = own ? '/api/readers/me/taste' : nickname ? '/api/readers/' + encodeURIComponent(nickname) + '/taste' : null

    useEffect(() => {
        if (!own) return
        const refresh = () => setVersion(v => v + 1)
        window.addEventListener('library-changed', refresh)
        window.addEventListener('achievements-changed', refresh)
        return () => { window.removeEventListener('library-changed', refresh); window.removeEventListener('achievements-changed', refresh) }
    }, [own])

    useEffect(() => {
        if (!url) return
        const controller = new AbortController()
        setFailed(false)
        const load = own ? request(url, { signal: controller.signal, cache: 'no-store' }) : fetch(url, { signal: controller.signal, cache: 'no-store' })
        void load.then(async response => {
            if (!response.ok) throw new Error('taste unavailable')
            const data = await response.json() as TasteProfile
            if (!controller.signal.aborted) setProfile(data)
        }).catch(() => { if (!controller.signal.aborted) setFailed(true) })
        return () => controller.abort()
    }, [own, request, url, version])

    return { profile, failed }
}
