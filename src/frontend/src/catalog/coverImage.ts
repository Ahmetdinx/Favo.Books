// Resize only known Open Library cover URLs; preserve edition identity and other providers.
export function catalogCoverUrl(source: string): string {
    try {
        const url = new URL(source)
        if (url.protocol !== 'https:' || url.hostname !== 'covers.openlibrary.org' || url.port
            || !/^\/b\/(id|olid|isbn|oclc|lccn)\/[^/]+-[SML]\.jpg$/i.test(url.pathname)) return source
        url.pathname = url.pathname.replace(/-[SML]\.jpg$/i, '-M.jpg')
        return url.toString()
    } catch { return source }
}
