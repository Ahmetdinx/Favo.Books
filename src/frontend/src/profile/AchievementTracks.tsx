import './Achievements.css'

type Badge = { threshold: number; title: string }
export type AchievementTrack = { kind: string; label: string; count: number; current: Badge | null;
    next: Badge | null; remaining: number; progressPercent: number; badges: Badge[] }

export default function AchievementTracks({ items, publicView = false }: { items: AchievementTrack[]; publicView?: boolean }) {
    return <section className="achievement-section" aria-label={publicView ? 'Okur başarıları' : 'Başarılarım'}>
        <div className="achievement-tracks">
            {items.map(track => <article key={track.kind} className={'achievement-card ' + track.kind}>
                <div className={'achievement-emblem' + (track.current ? ' unlocked' : '')} aria-hidden="true">
                    <svg viewBox="0 0 32 32" fill="none" stroke="currentColor" strokeWidth="1.5">
                        {track.kind === 'reading' ? <><path d="M16 8c-4-3-8-3-12-2v19c4-1 8-1 12 2 4-3 8-3 12-2V6c-4-1-8-1-12 2Z"/><path d="M16 8v19M8 11h4M20 11h4M8 15h4M20 15h4"/></>
                            : <><path d="m16 3 4 8 9 1-6.5 6.5L24 28l-8-4.5L8 28l1.5-9.5L3 12l9-1Z"/><path d="m11 16 3 3 7-7"/></>}
                    </svg>
                </div>
                <div className="achievement-content">
                    <div className="achievement-label"><span>{track.label}</span><strong>{track.count.toLocaleString('tr-TR')}</strong></div>
                    <h3>{track.current?.title ?? (publicView ? 'Henüz rozet yok' : track.kind === 'reading' ? 'İlk romanını bekliyor' : 'İlk değerlendirmeni bekliyor')}</h3>
                    {!publicView && <>
                        <progress max={100} value={track.progressPercent} aria-label={track.label + ': ' + (track.next ? track.next.title + ' rozetine ilerleme' : 'Tüm rozetler açıldı')} />
                        <p>{track.next ? <><strong>{track.next.title}</strong> için {track.remaining.toLocaleString('tr-TR')} {track.kind === 'reading' ? 'roman daha' : 'değerlendirme daha'}</> : 'Tüm rozetler açıldı.'}</p>
                    </>}
                    <details className="achievement-milestones">
                        <summary>{publicView ? 'Rozetler' : 'Hedefleri gör'}</summary>
                        <ol>{track.badges.map(badge => <li key={badge.threshold} className={track.count >= badge.threshold ? 'earned' : ''}>
                            <span aria-label={track.count >= badge.threshold ? 'Kazanıldı' : 'Henüz kazanılmadı'}>{track.count >= badge.threshold ? '✓' : '○'}</span>
                            <span>{badge.title}</span><strong>{badge.threshold.toLocaleString('tr-TR')}</strong>
                        </li>)}</ol>
                    </details>
                </div>
            </article>)}
        </div>
    </section>
}
