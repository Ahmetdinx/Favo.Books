export type RatingCriterionKey =
    | 'theme' | 'language' | 'characters' | 'structure'
    | 'pace' | 'impact' | 'experience'

export type RatingScores = Record<RatingCriterionKey, number>
export type RatingDraft = Record<RatingCriterionKey, number | ''>

export type RatingCriterion = {
    key: RatingCriterionKey
    label: string
    weight: number
    description: string
}

export type BookRatingSummary = {
    bookId: string
    averageScore: number | null
    ratingCount: number
    criteriaAverages: RatingScores | null
    currentUserRating: { scores: RatingScores; overallScore: number } | null
    legacyRatingCount: number
    currentUserLegacyScore: number | null
    minimumScore: number
    maximumScore: number
    criteria: RatingCriterion[]
}
