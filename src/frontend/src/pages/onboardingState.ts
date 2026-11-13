// Remembers, per reader and per browser, that the first-run guide was finished or skipped.
// Losing this (private window, cleared storage) only means the home page offers the guide again.
const key = (userId: string) => `favo:onboarding-seen:${userId}`

export function hasSeenOnboarding(userId: string) {
    try { return window.localStorage.getItem(key(userId)) === '1' } catch { return false }
}

export function markOnboardingSeen(userId: string) {
    try { window.localStorage.setItem(key(userId), '1') } catch { /* the home card simply stays offered */ }
}

/** Where a reader lands after signing in without a requested page: the guide while their library is empty. */
export async function firstRunPath(userId: string, accessToken: string, fallback: string) {
    if (hasSeenOnboarding(userId)) return fallback
    try {
        const response = await fetch('/api/reading/library/summary', { headers: { Authorization: `Bearer ${accessToken}` }, cache: 'no-store' })
        if (!response.ok) return fallback
        const summary = await response.json() as { totalCount: number }
        return summary.totalCount === 0 ? '/baslangic' : fallback
    } catch { return fallback }
}

// The reader whose shared profile brought a visitor here; after sign-up and the guide the visitor
// lands back on that profile to see their compatibility. Kept for a day so a later sign-up still counts.
const inviteKey = 'favo:invited-by'
const inviteTtl = 24 * 60 * 60 * 1000

export function rememberInvite(nickname: string) {
    try { window.localStorage.setItem(inviteKey, JSON.stringify({ nickname, at: Date.now() })) } catch { /* optional */ }
}

export function pendingInvite(): string | null {
    try {
        const value = JSON.parse(window.localStorage.getItem(inviteKey) ?? 'null') as { nickname?: string; at?: number } | null
        return value?.nickname && value.at && Date.now() - value.at < inviteTtl ? value.nickname : null
    } catch { return null }
}

export function clearInvite() {
    try { window.localStorage.removeItem(inviteKey) } catch { /* optional */ }
}
