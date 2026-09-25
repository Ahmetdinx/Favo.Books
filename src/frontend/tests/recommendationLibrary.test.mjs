import assert from 'node:assert/strict'
import { test } from 'node:test'
import { saveInterestedBook } from '../src/reading/readingApi.ts'

globalThis.window = new EventTarget()
const json = (body, status = 200) => new Response(JSON.stringify(body), { status })

test('interest adds a missing novel to WantToRead and announces the library change', async () => {
    let notified = 0
    const listener = () => notified++
    window.addEventListener('library-changed', listener)
    const calls = []
    const request = async (url, init) => {
        calls.push({ url, init })
        return init.method === 'GET' ? json({}, 404) : json({ libraryEntryId: 'entry' }, 201)
    }
    assert.equal(await saveInterestedBook(request, 'novel'), 'added')
    assert.deepEqual(JSON.parse(calls[1].init.body), { bookId: 'novel', status: 'WantToRead' })
    assert.equal(notified, 1)
    window.removeEventListener('library-changed', listener)
})

for (const status of ['WantToRead', 'Reading', 'Read', 'OnHold', 'Dropped']) {
    test(`interest preserves an existing ${status} entry`, async () => {
        let calls = 0
        const request = async (_, init) => {
            calls++
            assert.equal(init.method, 'GET')
            return json({ bookId: 'novel', status })
        }
        assert.equal(await saveInterestedBook(request, 'novel'), 'existing')
        assert.equal(calls, 1)
    })
}

test('a concurrent add is accepted only after verifying the library entry', async () => {
    const replies = [json({}, 404), json({}, 409), json({ status: 'Read' })]
    assert.equal(await saveInterestedBook(async () => replies.shift(), 'novel'), 'existing')
    assert.equal(replies.length, 0)
})

test('failed writes and unresolved conflicts are not reported as success', async () => {
    for (const replies of [[json({}, 404), json({}, 500)], [json({}, 404), json({}, 409), json({}, 404)]]) {
        await assert.rejects(saveInterestedBook(async () => replies.shift(), 'novel'))
    }
})
