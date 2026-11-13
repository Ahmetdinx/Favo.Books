import './ShareIdentity.css'
import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { downloadCardImage, type CardFormat } from './tasteCardImage'

const formats: { id: CardFormat; label: string; hint: string }[] = [
    { id: 'story', label: 'Hikâye', hint: '9:16 · Instagram, WhatsApp durum' },
    { id: 'post', label: 'Gönderi', hint: '4:5 · akış ve X' },
]

type Props = {
    open: boolean
    onClose: () => void
    title: string
    /** Accessible description of the card for the preview image. */
    alt: string
    /** Draws the card; null while its data is still loading. Changing the function redraws the preview. */
    render: ((format: CardFormat) => Promise<Blob>) | null
    /** Text and link attached when the system share sheet is used (the link carries ?ref for attribution). */
    shareText: string
    link?: string | null
    /** Link for "Linki kopyala" when it should be tagged differently from the shared one (defaults to link). */
    copyLink?: string | null
    fileName: string
    note: string
}

/**
 * Spotify-style share sheet used by every Favo share card: pick a format, see the exact image,
 * then share, download or copy the link. The preview is the same PNG that gets shared.
 */
export default function ShareSheet({ open, onClose, title, alt, render, shareText, link, copyLink: copyUrl, fileName, note }: Props) {
    const dialog = useRef<HTMLDialogElement>(null)
    const [format, setFormat] = useState<CardFormat>('story')
    const [image, setImage] = useState<{ blob: Blob; url: string; format: CardFormat } | null>(null)
    const [message, setMessage] = useState('')
    const [busy, setBusy] = useState(false)

    useEffect(() => {
        const element = dialog.current
        if (!element) return
        if (open && !element.open) element.showModal()
        if (!open && element.open) element.close()
    }, [open])

    useEffect(() => {
        if (!open || !render) return
        let active = true
        void render(format).then(async blob => {
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
    }, [open, render, format])

    const ready = image?.format === format
    async function share() {
        if (!image) return
        setBusy(true); setMessage('')
        try {
            const file = new File([image.blob], fileName, { type: 'image/png' })
            if (navigator.canShare?.({ files: [file] })) {
                try {
                    await navigator.share({ files: [file], title, text: link ? `${shareText} ${link}` : shareText })
                    setMessage('Paylaşıldı.')
                    return
                } catch (error) {
                    if (error instanceof DOMException && error.name === 'AbortError') return
                }
            }
            downloadCardImage(image.blob, fileName)
            setMessage('Kart indirildi. Hikâyene ya da gönderine ekleyebilirsin.')
        } finally { setBusy(false) }
    }
    async function copyLink() {
        const url = copyUrl ?? link
        if (!url) return
        try { await navigator.clipboard.writeText(url); setMessage('Link kopyalandı.') }
        catch { setMessage('Link kopyalanamadı.') }
    }

    // Portalled to the app root (keeps the theme) so the card it is opened from cannot restyle the dialog.
    return createPortal(<dialog ref={dialog} className="share-identity" aria-labelledby="share-sheet-title" onClose={onClose}
        onClick={event => { if (event.target === dialog.current) onClose() }}>
        <div className="share-identity-panel">
            <header>
                <h2 id="share-sheet-title">{title}</h2>
                <button type="button" className="share-identity-close" aria-label="Kapat" onClick={onClose}>✕</button>
            </header>
            <div className="share-identity-formats" role="radiogroup" aria-label="Görsel biçimi">
                {formats.map(option => <button key={option.id} type="button" role="radio" aria-checked={format === option.id}
                    className={format === option.id ? 'selected' : ''} onClick={() => setFormat(option.id)}>
                    <strong>{option.label}</strong><span>{option.hint}</span>
                </button>)}
            </div>
            <div className={`share-identity-preview ${format}`} aria-busy={!ready}>
                {ready ? <img src={image.url} alt={`${alt} (${format === 'story' ? 'hikâye' : 'gönderi'} önizlemesi)`} />
                    : <span role="status">Kart hazırlanıyor…</span>}
            </div>
            <div className="share-identity-actions">
                <button type="button" className="button primary-button" disabled={!ready || busy} onClick={() => void share()}>Paylaş</button>
                <button type="button" className="button secondary-button" disabled={!ready} onClick={() => image && downloadCardImage(image.blob, fileName)}>İndir</button>
                {link && <button type="button" className="button secondary-button" onClick={() => void copyLink()}>Linki kopyala</button>}
            </div>
            {message && <p className="share-identity-message" role="status">{message}</p>}
            <p className="share-identity-note">{note}</p>
        </div>
    </dialog>, document.querySelector('.app') ?? document.body)
}
