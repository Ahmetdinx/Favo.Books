import { useState } from 'react'
import { cleanBookTitle } from '../catalog/bookTitle'
import { catalogCoverUrl } from '../catalog/coverImage'
import './BookDesign.css'

type BookObjectProps = {
    title: string
    author?: string
    coverImageUrl: string | null | undefined
    size?: 'xs' | 'sm' | 'md' | 'lg'
    priority?: boolean
}

function coverHue(title: string) {
    let hash = 0
    for (const character of title) hash = (hash * 31 + character.charCodeAt(0)) % 360
    return hash
}

export default function BookObject({ title, author, coverImageUrl, size = 'md', priority = false }: BookObjectProps) {
    const [failedUrl, setFailedUrl] = useState<string | null>(null)
    const showImage = !!coverImageUrl && failedUrl !== coverImageUrl

    return <span className={`book-object book-object-${size}`}>
        {showImage
            ? <img src={catalogCoverUrl(coverImageUrl)} alt="" loading={priority ? 'eager' : 'lazy'} decoding="async"
                onError={() => setFailedUrl(coverImageUrl)} />
            : <span className="book-object-plain" style={{ '--book-hue': coverHue(title) } as React.CSSProperties}>
                <strong>{cleanBookTitle(title)}</strong>
                {author && <small>{author}</small>}
            </span>}
    </span>
}
