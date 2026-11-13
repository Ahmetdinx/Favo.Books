import { useCallback, useEffect, useState } from 'react'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getReaderSettings } from './profileVisibility'
import { renderCompatibilityCard, type CompatibilityShare } from './shareCards'
import ShareSheet from './ShareSheet'
import { loadSignatureBook, profileUrl, type CardFormat, type SignatureBook } from './tasteCardImage'

/** Share sheet for a compatibility result; the card links back to the sharing (viewing) reader. */
export default function ShareCompatibility({ open, onClose, other, percent, label, sharedGenres, commonBooks, suggestionId }: {
    open: boolean; onClose: () => void; other: string; percent: number; label: string | null
    sharedGenres: string[]; commonBooks: number; suggestionId: string | null
}) {
    const request = useAuthenticatedFetch()
    const [viewer, setViewer] = useState<{ nickname: string | null; isPublic: boolean } | undefined>(undefined)
    const [suggestion, setSuggestion] = useState<SignatureBook | null | undefined>(undefined)

    useEffect(() => {
        if (!open) return
        let active = true
        getReaderSettings(request).then(s => { if (active) setViewer({ nickname: s.nickname, isPublic: s.isPublic }) })
            .catch(() => { if (active) setViewer({ nickname: null, isPublic: false }) })
        void loadSignatureBook(suggestionId).then(book => { if (active) setSuggestion(book) })
        return () => { active = false }
    }, [open, request, suggestionId])

    const data: CompatibilityShare | null = viewer && suggestion !== undefined
        ? { viewer: viewer.nickname, other, percent, label, sharedGenres, commonBooks, suggestion } : null
    const linkable = !!viewer?.nickname && viewer.isPublic
    const draw = useCallback((format: CardFormat) => renderCompatibilityCard(data!, format, linkable),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [viewer, suggestion, other, percent, label, sharedGenres.join('|'), commonBooks, linkable])

    return <ShareSheet open={open} onClose={onClose} title="Uyumunu paylaş" alt={`@${other} ile %${percent} uyum kartı`}
        render={data ? draw : null} fileName={`favo-uyum-${other}.png`}
        shareText={`@${other} ile %${percent} uyumluyuz. Seninle uyumumuz kaç?`}
        link={linkable ? profileUrl(viewer!.nickname!, 'uyum') : null}
        note={linkable ? 'Kartta senin profil adresin yazar; gören kişi seninle uyumunu çıkarabilir.'
            : 'Profilin gizli olduğu için kartta yalnızca Favo adresi yazar.'} />
}
