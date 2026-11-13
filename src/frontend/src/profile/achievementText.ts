type Badge = { threshold: number; title: string }
export type AchievementTrack = { kind: string; label: string; count: number; current: Badge | null;
    next: Badge | null; remaining: number; progressPercent: number; badges: Badge[] }

export function emptyBadgeTitle(track: AchievementTrack, publicView: boolean) {
    return publicView ? 'Henüz rozet yok' : track.kind === 'reading' ? 'İlk romanını bekliyor' : 'İlk değerlendirmeni bekliyor'
}
