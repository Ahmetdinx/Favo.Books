import type { RatingCriterion, RatingDraft, RatingScores } from './communityTypes'

const keys = ['theme', 'language', 'characters', 'structure', 'pace', 'impact', 'experience'] as const

export function emptyRatingDraft(): RatingDraft {
    return { theme: '', language: '', characters: '', structure: '', pace: '', impact: '', experience: '' }
}

export function toRatingScores(draft: RatingDraft): RatingScores | null {
    if (!keys.every((key) =>
        typeof draft[key] === 'number' && Number.isInteger(draft[key]) &&
        draft[key] >= 1 && draft[key] <= 10)) return null
    return draft as RatingScores
}

export function previewOverallScore(draft: RatingDraft, criteria: RatingCriterion[]): number | null {
    const scores = toRatingScores(draft)
    if (!scores || criteria.length !== keys.length ||
        new Set(criteria.map((criterion) => criterion.key)).size !== keys.length ||
        !criteria.every((criterion) => keys.includes(criterion.key)) ||
        criteria.reduce((sum, criterion) => sum + criterion.weight, 0) !== 100) return null
    return criteria.reduce((sum, criterion) => sum + scores[criterion.key] * criterion.weight, 0) / 100
}
