import './ShareIdentity.css'
import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getReaderSettings } from './profileVisibility'
import {
    downloadCardImage, loadSignatureBook, profileUrl, renderTasteCard, shareCardImage,
    type CardFormat, type SignatureBook, type TasteProfile,
} from './tasteCardImage'

const formats: { id: CardFormat; label: string; hint: string }[] = [
    { id: 'story', label: 'Hikâye', hint: '9:16 · Instagram, WhatsApp durum' },
    { id: 'post', label: 'Gönderi', hint: '4:5 · akış ve X' },
]

/**
 * Spotify-style share sheet for the reader identity: pick a format, see the exact image, then share,
 * download or copy the profile link. The preview is the same PNG that gets shared.
 */
export default function ShareIdentity({ profile, nickname, open, onClose }: {
    profile: TasteProfile; nickname: string | null; open: boolean; onClose: () => void
}) {
    const dialog = useRef<HTMLDialogElement>(null)
    const [format, setFormat] = useState<CardFormat>('story')
    const [signature, setSignature] = useState<SignatureBook | null | undefined>(undefined)
    const [image, setImage] = useState<{ blob: Blob; url: string; format: CardFormat } | null>(null)
    const [message, setMessage] = useState('')
    const [busy, setBusy] = useState(false)
    const request = useAuthenticatedFetch()
    // A private profile's address leads nowhere, so it is left off the card until the reader opens it.
    const [isPublic, setIsPublic] = useState<boolean | undefined>(undefined)

    useEffect(() => {
        const element = dialog.current
        if (!element) return
        if (open && !element.open) element.showModal()
        if (!open && element.open) element.close()
    }, [open])

    useEffect(() => {
        if (!open) return
        let active = true
        void loadSignatureBook(profile.signatureBookId).then(book => { if (active) setSignature(book) })
        getReaderSettings(request).then(settings => { if (active) setIsPublic(settings.isPublic) })
            .catch(() => { if (active) setIsPublic(false) })
        return () => { active = false }
    }, [open, profile.signatureBookId, request])

    useEffect(() => {
        if (!open || signature === undefined || isPublic === undefined) return
        let active = true
        void renderTasteCard(profile, nickname, format, signature, isPublic).then(async blob => {
            // A data URL has no lifetime to manage, unlike an object URL that may be revoked under the <img>.
            const url = await new Promise<string>((resolve, reject) => {
                const reader = new FileReader()
                reader.onload = () => resolve(reader.result as string)
                reader.onerror = () => reject(reader.error)
                reader.readAsDataURL(blob)
            })
            if (active) setImage({ blob, url, format })
        }).catch(() => { if (active) setMessage('Kart hazırlanamadı.') })
        return () => { active = false }
    }, [open, profile, nickname, format, signature, isPublic])

    const ready = image?.format === format
    async function share() {
        if (!image) return
        setBusy(true); setMessage('')
        try { setMessage(await shareCardImage(image.blob, nickname, !!isPublic) === 'shared' ? 'Paylaşıldı.' : 'Kart indirildi. Hikâyene ya da gönderine ekleyebilirsin.') }
        finally { setBusy(false) }
    }
    async function copyLink() {
        if (!nickname) return
        try { await navigator.clipboard.writeText(profileUrl(nickname)); setMessage('Profil linkin kopyalandı.') }
        catch { setMessage('Link kopyalanamadı.') }
    }

    // Portalled to the app root (keeps the theme) so the card it is opened from cannot restyle the dialog.
    return createPortal(<dialog ref={dialog} className="share-identity" aria-labelledby="share-identity-title" onClose={onClose}
        onClick={event => { if (event.target === dialog.current) onClose() }}>
        <div className="share-identity-panel">
            <header>
                <h2 id="share-identity-title">Okur kimliğini paylaş</h2>
                <button type="button" className="share-identity-close" aria-label="Kapat" onClick={onClose}>✕</button>
            </header>
            <div className="share-identity-formats" role="radiogroup" aria-label="Görsel biçimi">
                {formats.map(option => <button key={option.id} type="button" role="radio" aria-checked={format === option.id}
                    className={format === option.id ? 'selected' : ''} onClick={() => setFormat(option.id)}>
                    <strong>{option.label}</strong><span>{option.hint}</span>
                </button>)}
            </div>
            <div className={`share-identity-preview ${format}`} aria-busy={!ready}>
                {ready ? <img src={image.url} alt={`${profile.title ?? 'Okur kimliği'} kartının ${format === 'story' ? 'hikâye' : 'gönderi'} önizlemesi`} />
                    : <span role="status">Kart hazırlanıyor…</span>}
            </div>
            <div className="share-identity-actions">
                <button type="button" className="button primary-button" disabled={!ready || busy} onClick={() => void share()}>Paylaş</button>
                <button type="button" className="button secondary-button" disabled={!ready} onClick={() => image && downloadCardImage(image.blob)}>İndir</button>
                {nickname && isPublic && <button type="button" className="button secondary-button" onClick={() => void copyLink()}>Linki kopyala</button>}
            </div>
            {message && <p className="share-identity-message" role="status">{message}</p>}
            <p className="share-identity-note">{isPublic === false
                ? 'Profilin gizli olduğu için kartta yalnızca Favo adresi yazar. Profilini herkese açarsan kartı gören kişi doğrudan seni bulur.'
                : 'Kartta profil adresin yazar; gören kişi seni bulabilir ve kendi okur kimliğini çıkarabilir.'}</p>
        </div>
    </dialog>, document.querySelector('.app') ?? document.body)
}
