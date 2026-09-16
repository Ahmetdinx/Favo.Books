import assert from 'node:assert/strict'
import test from 'node:test'
import { emptyRatingDraft, toRatingScores, previewOverallScore } from '../src/community/ratingCalculations.ts'

const keys = ['theme', 'language', 'characters', 'structure', 'pace', 'impact', 'experience']
const criteria = keys.map(key => ({ key, weight: key === 'pace' ? 10 : 15 }))
const complete = { theme: 8, language: 9, characters: 7, structure: 8, pace: 6, impact: 9, experience: 8 }

test('weighted preview follows the seven criteria and requires every answer', () => {
    assert.equal(previewOverallScore(complete, criteria), 7.95)
    assert.equal(previewOverallScore(emptyRatingDraft(), criteria), null)
    for (const key of keys) {
        for (const invalid of ['', 0, 11, 2.5, NaN, '8', undefined]) {
            assert.equal(toRatingScores({ ...complete, [key]: invalid }), null)
        }
    }
})
test('malformed criterion metadata cannot produce a misleading preview', () => {
    assert.equal(previewOverallScore(complete, criteria.slice(1)), null)
    assert.equal(previewOverallScore(complete, criteria.map(c => ({ ...c, weight: 10 }))), null)
    assert.equal(previewOverallScore(complete, [...criteria.slice(0, 6), criteria[0]]), null)
})
