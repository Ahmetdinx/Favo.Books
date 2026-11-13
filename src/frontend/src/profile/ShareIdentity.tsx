import { useCallback, useEffect, useState } from 'react'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getReaderSettings } from './profileVisibility'
import ShareSheet from './ShareSheet'
import { loadSignatureBook, profileUrl, renderTasteCard, type CardFormat, type SignatureBook, type TasteProfile } from './tasteCardImage'

/** Share sheet for the reader identity card. */
export default function ShareIdentity({ profile, nickname, open, onClose }: {
    profile: TasteProfile; nickname: string | null; open: boolean; onClose: () => void
}) {
    const request = useAuthenticatedFetch()
    const [signature, setSignature] = useState<SignatureBook | null | undefined>(undefined)
    // A private profile's address leads nowhere, so it is left off the card until the reader opens it.
    const [isPublic, setIsPublic] = useState<boolean | undefined>(undefined)

    useEffect(() => {
        if (!open) return
        let active = true
        void loadSignatureBook(profile.signatureBookId).then(book => { if (active) setSignature(book) })
        getReaderSettings(request).then(settings => { if (active) setIsPublic(settings.isPublic) })
            .catch(() => { if (active) setIsPublic(false) })
        return () => { active = false }
    }, [open, profile.signatureBookId, request])

    const draw = useCallback((format: CardFormat) =>
        renderTasteCard(profile, nickname, format, signature ?? null, !!isPublic), [profile, nickname, signature, isPublic])
    const linkable = !!nickname && !!isPublic

    return <ShareSheet open={open} onClose={onClose} title="Okur kimliğini paylaş" alt={`${profile.title ?? 'Okur kimliği'} kartı`}
        render={signature === undefined || isPublic === undefined ? null : draw}
        shareText="Okur kimliğim Favo'da:" link={linkable ? profileUrl(nickname, 'kart') : null} copyLink={linkable ? profileUrl(nickname, 'link') : null} fileName="favo-okur-kimligi.png"
        note={isPublic === false
            ? 'Profilin gizli olduğu için kartta yalnızca Favo adresi yazar. Profilini herkese açarsan kartı gören kişi doğrudan seni bulur.'
            : 'Kartta profil adresin yazar; gören kişi seni bulabilir ve kendi okur kimliğini çıkarabilir.'} />
}
