// Resize only known provider cover URLs, preserving the original edition's image.
export function catalogCoverUrl(source: string): string {
    try {
        const url = new URL(source)
        if (url.protocol === 'https:' && url.hostname === 'www.dogankitap.com.tr' && !url.port
            && !url.username && !url.password && url.pathname.startsWith('/files/kitaplar/img/')) {
            const thumbnail = new URL('/t', url.origin)
            thumbnail.searchParams.set('src', decodeURIComponent(url.pathname))
            thumbnail.searchParams.set('w', '250')
            thumbnail.searchParams.set('zc', '1')
            return thumbnail.toString()
        }
        if (url.protocol !== 'https:' || url.hostname !== 'covers.openlibrary.org' || url.port
            || !/^\/b\/(id|olid|isbn|oclc|lccn)\/[^/]+-[SML]\.jpg$/i.test(url.pathname)) return source
        url.pathname = url.pathname.replace(/-[SML]\.jpg$/i, '-M.jpg')
        return url.toString()
    } catch { return source }
}
