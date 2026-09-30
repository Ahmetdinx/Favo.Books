import { useEffect, useRef, useState } from 'react'
import type { AchievementTrack } from './AchievementTracks'
import ReaderAvatar from './ReaderAvatar'
import './AchievementCelebration.css'

export default function AchievementCelebration({ userId, items, avatarId }: { userId: string; items: AchievementTrack[]; avatarId?: string }) {
    const previous = useRef<Record<string, number> | null>(null)
    const [earned, setEarned] = useState<AchievementTrack[]>([])
    useEffect(() => {
        if (!items.length) return
        // Preserve celebration history across the Favo rebrand.
        const key = `kitapkivo:celebrations:${userId}`
        if (previous.current === null) {
            try {
                const stored = JSON.parse(localStorage.getItem(key) ?? 'null')
                if (stored && typeof stored === 'object' && !Array.isArray(stored)
                    && Object.values(stored).every(value => typeof value === 'number' && Number.isFinite(value) && value >= 0)) previous.current = stored
            } catch { /* storage is optional */ }
            if (previous.current === null) {
                previous.current = Object.fromEntries(items.map(t => [t.kind, t.current?.threshold ?? 0]))
            }
        }
        const gained = items.filter(t => (t.current?.threshold ?? 0) > (previous.current?.[t.kind] ?? 0))
        if (gained.length) setEarned(current => [...current.filter(t => !gained.some(g => g.kind === t.kind)), ...gained])
        for (const item of items) previous.current![item.kind] = Math.max(previous.current![item.kind] ?? 0, item.current?.threshold ?? 0)
        try { localStorage.setItem(key, JSON.stringify(previous.current)) } catch { /* keep this session's baseline */ }
    }, [items, userId])
    if (!earned.length) return null
    return <aside className="achievement-celebration" aria-label="Yeni başarı">
        <button type="button" className="celebration-close" aria-label="Kutlamayı kapat" onClick={() => setEarned([])}>×</button>
        <ReaderAvatar avatarId={avatarId} achievements={items} size="medium" />
        <div role="status" aria-live="polite"><small>Yeni rozetin hazır!</small>
            {earned.map(track => <div key={track.kind}><strong>{track.current?.title}</strong>
                <p>{track.next ? `${track.next.title} için ${track.remaining} ${track.kind === 'reading' ? 'roman' : 'değerlendirme'} daha.` : 'Bu serideki tüm rozetleri açtın.'}</p></div>)}
        </div>
    </aside>
}
