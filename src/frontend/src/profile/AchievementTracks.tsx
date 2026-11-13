import './Achievements.css'
import { emptyBadgeTitle } from './achievementText'
import type { AchievementTrack } from './achievementText'
export type { AchievementTrack } from './achievementText'

export function AchievementEmblem({ track }: { track: AchievementTrack }) {
    return <div className={'achievement-emblem' + (track.current ? ' unlocked' : '')} aria-hidden="true">
        <svg viewBox="0 0 32 32" fill="none" stroke="currentColor" strokeWidth="1.5">
            {track.kind === 'reading' ? <><path d="M16 8c-4-3-8-3-12-2v19c4-1 8-1 12 2 4-3 8-3 12-2V6c-4-1-8-1-12 2Z"/><path d="M16 8v19M8 11h4M20 11h4M8 15h4M20 15h4"/></>
                : <><path d="m16 3 4 8 9 1-6.5 6.5L24 28l-8-4.5L8 28l1.5-9.5L3 12l9-1Z"/><path d="m11 16 3 3 7-7"/></>}
        </svg>
    </div>
}

/** One badge track. `openMilestones` shows the full ladder (used inside popovers). */
export function AchievementCard({ track, publicView = false, openMilestones = false }: { track: AchievementTrack; publicView?: boolean; openMilestones?: boolean }) {
    const milestones = <ol>{track.badges.map(badge => <li key={badge.threshold} className={track.count >= badge.threshold ? 'earned' : ''}>
        <span aria-label={track.count >= badge.threshold ? 'Kazanıldı' : 'Henüz kazanılmadı'}>{track.count >= badge.threshold ? '✓' : '○'}</span>
        <span>{badge.title}</span><strong>{badge.threshold.toLocaleString('tr-TR')}</strong>
    </li>)}</ol>
    return <article className={'achievement-card ' + track.kind}>
        <AchievementEmblem track={track} />
        <div className="achievement-content">
            <div className="achievement-label"><span>{track.label}</span><strong>{track.count.toLocaleString('tr-TR')}</strong></div>
            <h3>{track.current?.title ?? emptyBadgeTitle(track, publicView)}</h3>
            {!publicView && <>
                <progress max={100} value={track.progressPercent} aria-label={track.label + ': ' + (track.next ? track.next.title + ' rozetine ilerleme' : 'Tüm rozetler açıldı')} />
                <p>{track.next ? <><strong>{track.next.title}</strong> için {track.remaining.toLocaleString('tr-TR')} {track.kind === 'reading' ? 'roman daha' : 'değerlendirme daha'}</> : 'Tüm rozetler açıldı.'}</p>
            </>}
            {openMilestones
                ? <div className="achievement-milestones">{milestones}</div>
                : <details className="achievement-milestones">
                    <summary>{publicView ? 'Rozetler' : 'Hedefleri gör'}</summary>
                    {milestones}
                </details>}
        </div>
    </article>
}

export default function AchievementTracks({ items, publicView = false }: { items: AchievementTrack[]; publicView?: boolean }) {
    return <section className="achievement-section" aria-label={publicView ? 'Okur başarıları' : 'Başarılarım'}>
        <div className="achievement-tracks">
            {items.map(track => <AchievementCard key={track.kind} track={track} publicView={publicView} />)}
        </div>
    </section>
}
