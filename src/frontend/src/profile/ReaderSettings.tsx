import { useContext, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import './Readers.css'
import ReaderAvatar from './ReaderAvatar'
import { avatars, ReaderBadgesContext } from './avatarOptions'

type Settings = { nickname: string | null; isPublic: boolean; avatarId: string }

/** `embedded`: only the form, for the settings panel in the reader band (the band shows identity and links). */
export default function ReaderSettings({ embedded = false }: { embedded?: boolean }) {
    const authFetch = useAuthenticatedFetch()
    const achievements = useContext(ReaderBadgesContext)
    const [avatarId, setAvatarId] = useState('owl')
    const [saved, setSaved] = useState<Settings | null>(null)
    const [nickname, setNickname] = useState('')
    const [isPublic, setIsPublic] = useState(false)
    const [message, setMessage] = useState('')
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(false)
    const [version, setVersion] = useState(0)
    useEffect(() => {
        const controller = new AbortController()
        setError('')
        authFetch('/api/readers/me', { signal: controller.signal }).then(async response => {
            if (!response.ok) throw new Error('Profil ayarları yüklenemedi.')
            const data = await response.json() as Settings
            if (controller.signal.aborted) return
            setSaved(data); setNickname(data.nickname ?? ''); setIsPublic(data.isPublic); setAvatarId(data.avatarId)
        }).catch(e => { if (!controller.signal.aborted) setError(e.message) })
        return () => controller.abort()
    }, [authFetch, version])

    async function save(event: FormEvent) {
        event.preventDefault(); setBusy(true); setError(''); setMessage('')
        try {
            const response = await authFetch('/api/readers/me', { method: 'PUT',
                headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nickname, isPublic, avatarId }) })
            if (!response.ok) {
                const body = await response.json().catch(() => ({}))
                throw new Error(body.message ?? 'Ayarlar kaydedilemedi.')
            }
            const data = await response.json() as Settings
            setSaved(data); setNickname(data.nickname ?? ''); setIsPublic(data.isPublic); setAvatarId(data.avatarId)
            setMessage('Kaydedildi.')
            window.dispatchEvent(new Event('reader-settings-changed'))
        } catch (e) { setError(e instanceof Error ? e.message : 'Kaydedilemedi.') }
        finally { setBusy(false) }
    }
    async function copy() {
        try {
            await navigator.clipboard.writeText(new URL(import.meta.env.BASE_URL + 'okur/' + saved!.nickname, window.location.origin).href)
            setMessage('Profil bağlantısı kopyalandı.')
        } catch { setError('Bağlantı kopyalanamadı. Profili açıp adres çubuğundan kopyalayabilirsin.') }
    }
    const form = saved && <form onSubmit={save} className="reader-settings-form">
                        <div className="avatar-preview">
                            <ReaderAvatar avatarId={avatarId} achievements={achievements} size="large" />
                            <div><strong>Okur avatarın</strong><small>Halkanın rengi okuma, yıldız değerlendirme rozetin.</small></div>
                        </div>
                        <fieldset className="avatar-picker" disabled={busy}>
                            <legend>Avatar seç</legend>
                            <div className="avatar-options">{avatars.map(avatar => <label key={avatar.id} className="avatar-option">
                                <input type="radio" name="avatar" value={avatar.id} checked={avatarId === avatar.id} onChange={() => setAvatarId(avatar.id)} />
                                <span aria-hidden="true"><ReaderAvatar avatarId={avatar.id} size="small" /></span>
                                <span>{avatar.label}</span>
                            </label>)}</div>
                        </fieldset>
                        <label htmlFor="reader-nickname">Kullanıcı adı</label>
                        <input id="reader-nickname" autoComplete="username" value={nickname} onChange={e => setNickname(e.target.value)}
                            minLength={3} maxLength={24} pattern="[a-zA-Z][a-zA-Z0-9_]{2,23}" required disabled={busy} aria-describedby="nickname-help" />
                        <small id="nickname-help">3–24 karakter; a-z, rakam ve alt çizgi. Bir harfle başla.</small>
                        <label className="reader-privacy"><input type="checkbox" checked={isPublic} disabled={busy} onChange={e => setIsPublic(e.target.checked)} /> Profilim herkese açık olsun</label>
                        <small>Kitaplığın ve puanların paylaşılır. E-posta adresin ve kişisel notların gizli kalır. Kullanıcı adını değiştirirsen eski profil bağlantın kapanır.</small>
                        <button className="button primary-button" disabled={busy}>{busy ? 'Kaydediliyor…' : 'Kaydet'}</button>
                    </form>
    if (embedded) return <section className="reader-settings reader-settings-embedded" aria-label="Profil ayarları">
        {saved ? form : !error && <p role="status">Profil ayarları yükleniyor…</p>}
        {error && <div role="alert">{error} {!saved && <button onClick={() => setVersion(v => v + 1)}>Tekrar dene</button>}</div>}
        {message && <p role="status">{message}</p>}
    </section>
    return <section className="reader-settings" aria-label="Profil kimliği ve gizlilik">
        {saved ? <>
            <div className="reader-settings-heading">
                <ReaderAvatar avatarId={saved.avatarId} achievements={achievements} />
                <strong>{saved.nickname ? '@' + saved.nickname : 'Kullanıcı adını seç'}</strong>
                <span>{saved.isPublic ? 'Herkese açık' : 'Gizli profil'}</span>
                {saved.nickname && saved.isPublic && <><Link to={'/okur/' + saved.nickname}>Profili gör</Link><button type="button" onClick={copy}>Bağlantıyı kopyala</button></>}
            </div>
            <details open={!saved.nickname || undefined}>
                <summary>Profil ayarları</summary>
                {form}
            </details>
        </> : !error && <p role="status">Profil ayarları yükleniyor…</p>}
        {error && <div role="alert">{error} {!saved && <button onClick={() => setVersion(v => v + 1)}>Tekrar dene</button>}</div>}
        {message && <p role="status">{message}</p>}
    </section>
}
