import assert from 'node:assert/strict'
import test from 'node:test'
import { readRatingDraft, writeRatingDraft, clearRatingDraft } from '../src/community/ratingDraftStorage.ts'
import { emptyRatingDraft, toRatingScores } from '../src/community/ratingCalculations.ts'
const draft = { ...emptyRatingDraft(), theme: 8, pace: 3 }
function storage() {
    const data = new Map()
    return { getItem: key => data.get(key) ?? null, setItem: (key, value) => data.set(key, value), removeItem: key => data.delete(key) }
}
test('partial drafts survive reopening without becoming completed ratings', () => {
    const store = storage()
    assert.equal(writeRatingDraft(store, 'user', 'book', draft), true)
    assert.deepEqual(readRatingDraft(store, 'user', 'book'), draft)
    assert.equal(toRatingScores(readRatingDraft(store, 'user', 'book')), null)
})
test('drafts are isolated by account and book, and clearing affects only one draft', () => {
    const store = storage()
    writeRatingDraft(store, 'a', 'one', draft)
    writeRatingDraft(store, 'b', 'one', draft)
    assert.equal(readRatingDraft(store, 'a', 'two'), null)
    assert.equal(readRatingDraft(store, 'c', 'one'), null)
    assert.equal(clearRatingDraft(store, 'a', 'one'), true)
    assert.equal(readRatingDraft(store, 'a', 'one'), null)
    assert.deepEqual(readRatingDraft(store, 'b', 'one'), draft)
})
test('invalid stored data is ignored, including missing and out-of-range criteria', () => {
    for (const raw of ['broken', 'null', '{}', JSON.stringify({ ...draft, pace: 11 }), JSON.stringify({ ...draft, pace: '7' })]) {
        assert.equal(readRatingDraft({ getItem: () => raw }, 'a', 'b'), null)
    }
})
test('blocked storage does not crash the editor', () => {
    const denied = () => { throw Error('Storage denied') }
    assert.equal(readRatingDraft({ getItem: denied }, 'a', 'b'), null)
    assert.equal(writeRatingDraft({ setItem: denied }, 'a', 'b', draft), false)
    assert.equal(clearRatingDraft({ removeItem: denied }, 'a', 'b'), false)
})
