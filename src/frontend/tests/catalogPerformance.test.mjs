import assert from 'node:assert/strict'
import test from 'node:test'
import { catalogCoverUrl } from '../src/catalog/coverImage.ts'
import { createCatalogCache } from '../src/catalog/catalogCache.ts'
import { browseBooks } from '../src/catalog/catalogApi.ts'

test('catalog thumbnails keep edition identity and query parameters', () => {
    assert.equal(catalogCoverUrl('https://covers.openlibrary.org/b/id/123-L.jpg?default=false'),
        'https://covers.openlibrary.org/b/id/123-M.jpg?default=false')
    assert.equal(catalogCoverUrl('https://covers.openlibrary.org/b/olid/OL123M-L.jpg'),
        'https://covers.openlibrary.org/b/olid/OL123M-M.jpg')
    for (const url of ['https://example.com/123-L.jpg', 'https://covers.openlibrary.org.evil.test/b/id/123-L.jpg', '/local.jpg', 'invalid'])
        assert.equal(catalogCoverUrl(url), url)
})

test('page cache expires, evicts least recently used entries and separates filters', () => {
    let now = 0
    const cache = createCatalogCache(2, 100, () => now)
    cache.set('page=1&category=roman', 'roman')
    cache.set('page=1&category=siir', 'siir')
    assert.equal(cache.get('page=1&category=roman'), 'roman')
    cache.set('page=2&category=roman', 'next')
    assert.equal(cache.get('page=1&category=siir'), undefined)
    now = 100
    assert.equal(cache.get('page=1&category=roman'), undefined)
    cache.set('page=2&category=roman', 'updated')
    assert.equal(cache.get('page=2&category=roman'), 'updated')
})

test('publisher catalog thumbnails use the published resize route without changing the cover', () => {
    const result = new URL(catalogCoverUrl('https://www.dogankitap.com.tr/files/kitaplar/img/roman%20kapak.jpg'))
    assert.equal(result.origin, 'https://www.dogankitap.com.tr')
    assert.equal(result.pathname, '/t')
    assert.equal(result.searchParams.get('src'), '/files/kitaplar/img/roman kapak.jpg')
    assert.equal(result.searchParams.get('w'), '250')
    for (const source of ['https://www.dogankitap.com.tr.evil.test/files/kitaplar/img/a.jpg',
        'https://www.dogankitap.com.tr/other/a.jpg', 'https://img.iskultur.com.tr/webp/cover.jpg'])
        assert.equal(catalogCoverUrl(source), source)
})

test('catalog API reuses cached pages, separates filters, honors cancellation and retries errors', async () => {
    const originalFetch = globalThis.fetch
    let calls = 0
    let fail = false
    globalThis.fetch = async () => {
        calls++
        return new Response(JSON.stringify({ items: [], totalCount: calls }), { status: fail ? 503 : 200 })
    }
    const query = { query: 'cache-test', category: '', searchField: 'all', page: 1, pageSize: 12, sort: 'title' }
    try {
        await browseBooks(query)
        await browseBooks(query)
        assert.equal(calls, 1)
        await browseBooks({ ...query, category: 'roman' })
        assert.equal(calls, 2)
        await browseBooks(query, undefined, true)
        assert.equal(calls, 3)
        const controller = new AbortController()
        controller.abort()
        await assert.rejects(browseBooks(query, controller.signal), { name: 'AbortError' })
        assert.equal(calls, 3)
        fail = true
        await assert.rejects(browseBooks({ ...query, page: 2 }))
        fail = false
        await browseBooks({ ...query, page: 2 })
        assert.equal(calls, 5)
    } finally { globalThis.fetch = originalFetch }
})
