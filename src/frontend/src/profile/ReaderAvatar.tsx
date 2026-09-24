import type { AchievementTrack } from './AchievementTracks'
import { avatars } from './avatarOptions'
import './ReaderAvatar.css'

const readingSymbols: Record<number, string> = { 1: '📖', 10: '📚', 100: '🐛', 1000: '🏛️' }
const evaluationSymbols: Record<number, string> = { 1: '✍️', 10: '🔍', 100: '🪶', 1000: '🏅' }

export default function ReaderAvatar({ avatarId = 'owl', achievements = [], size = 'medium' }: {
    avatarId?: string; achievements?: AchievementTrack[]; size?: 'small' | 'medium' | 'large'
}) {
    const avatar = avatars.find(a => a.id === avatarId) ?? avatars[0]
    const reading = achievements.find(t => t.kind === 'reading')?.current
    const evaluation = achievements.find(t => t.kind === 'evaluation')?.current
    const description = [avatar.label + ' avatarı', reading && 'Okuma: ' + reading.title, evaluation && 'Değerlendirme: ' + evaluation.title].filter(Boolean).join(' · ')
    return <span className={'reader-avatar-display ' + size} data-reading={reading?.threshold ?? 0}
        role="img" aria-label={description} title={description}>
        <span className={'reader-avatar-face avatar-' + avatar.id} aria-hidden="true">{avatar.emoji}</span>
        {reading && <span className="avatar-badge reading-badge" data-level={reading.threshold} aria-hidden="true">{readingSymbols[reading.threshold] ?? '📖'}</span>}
        {evaluation && <span className="avatar-badge evaluation-badge" data-level={evaluation.threshold} aria-hidden="true">{evaluationSymbols[evaluation.threshold] ?? '✍️'}</span>}
    </span>
}
