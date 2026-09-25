import type { RatingDraft } from './communityTypes'

const keys = ['theme', 'language', 'characters', 'structure', 'pace', 'impact', 'experience'] as const
const storageKey = (userId: string, bookId: string) => `kitapkivo:rating-draft:v1:${encodeURIComponent(userId)}:${encodeURIComponent(bookId)}`

export function readRatingDraft(storage: Pick<Storage, 'getItem'>, userId: string, bookId: string): RatingDraft | null {
    try {
        const value = JSON.parse(storage.getItem(storageKey(userId, bookId)) ?? 'null')
        if (!value || !keys.every(key => value[key] === '' ||
            typeof value[key] === 'number' && Number.isInteger(value[key]) && value[key] >= 1 && value[key] <= 10)) return null
        return Object.fromEntries(keys.map(key => [key, value[key]])) as RatingDraft
    } catch { return null }
}

export function writeRatingDraft(storage: Pick<Storage, 'setItem'>, userId: string, bookId: string, draft: RatingDraft): boolean {
    try { storage.setItem(storageKey(userId, bookId), JSON.stringify(draft)); return true }
    catch { return false }
}

export function clearRatingDraft(storage: Pick<Storage, 'removeItem'>, userId: string, bookId: string): boolean {
    try { storage.removeItem(storageKey(userId, bookId)); return true }
    catch { return false }
}

export const criterionHints = {
    theme: 'Fikri ne kadar ilgi çekici buldun?',
    language: 'Dili ve anlatımı okumayı keyifli kıldı mı?',
    characters: 'Karakterler sana inandırıcı geldi mi?',
    structure: 'Olaylar birbirine tutarlı biçimde bağlandı mı?',
    pace: 'Bir sonraki sayfayı merak ettin mi?',
    impact: 'Sende bir duygu ya da düşünce bıraktı mı?',
    experience: 'Bir bütün olarak okumaktan ne kadar keyif aldın?',
} as const

export function scoreLabel(score: number): string {
    if (score <= 2) return 'Çok zayıf'
    if (score <= 4) return 'Zayıf'
    if (score <= 6) return 'Orta'
    if (score <= 8) return 'İyi'
    return 'Çok iyi'
}
