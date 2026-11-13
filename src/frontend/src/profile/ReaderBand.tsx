import { useContext, useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { AchievementCard, AchievementEmblem } from './AchievementTracks'
import { emptyBadgeTitle } from './achievementText'
import { ReaderBadgesContext } from './avatarOptions'
import Popover from './Popover'
import ReaderAvatar from './ReaderAvatar'
import ReaderSettings from './ReaderSettings'
import { ReaderConnections } from './ReaderSocial'
import { TasteCardView } from './TasteCard'
import ShareIdentity from './ShareIdentity'
import { useTasteProfile } from './useTasteProfile'
import './ReaderBand.css'

type Settings = { nickname: string | null; isPublic: boolean; avatarId: string }

/** Top of the own profile: who I am, my identity card and badges (as popovers), my circle and my settings. */
export default function ReaderBand() {
    const request = useAuthenticatedFetch()
    const achievements = useContext(ReaderBadgesContext)
    const { profile } = useTasteProfile(true)
    const [settings, setSettings] = useState<Settings | null>(null)
    const [settingsOpen, setSettingsOpen] = useState(false)
    const [sharing, setSharing] = useState(false)
    const [message, setMessage] = useState('')
    const [version, setVersion] = useState(0)

    useEffect(() => {
        const refresh = () => setVersion(v => v + 1)
        window.addEventListener('reader-settings-changed', refresh)
        return () => window.removeEventListener('reader-settings-changed', refresh)
    }, [])
    useEffect(() => {
        const controller = new AbortController()
        void request('/api/readers/me', { signal: controller.signal, cache: 'no-store' })
            .then(r => r.ok ? r.json() as Promise<Settings> : null)
            .then(data => {
                if (!data || controller.signal.aborted) return
                setSettings(data)
                if (!data.nickname) setSettingsOpen(true)
            }).catch(() => { /* the band still shows the rest */ })
        return () => controller.abort()
    }, [request, version])

    async function copyLink() {
        if (!settings?.nickname) return
        try {
            await navigator.clipboard.writeText(new URL(import.meta.env.BASE_URL + 'okur/' + settings.nickname, window.location.origin).href)
            setMessage('Profil bağlantısı kopyalandı.')
        } catch { setMessage('Bağlantı kopyalanamadı.') }
    }

    return <section className="reader-band" aria-labelledby="reader-band-heading">
        <div className="reader-band-top">
            <h2 id="reader-band-heading" className="reader-band-kicker">Okur çevrem</h2>
            <button type="button" className="band-settings" aria-expanded={settingsOpen} aria-controls="reader-band-settings"
                onClick={() => setSettingsOpen(o => !o)}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
                    <circle cx="12" cy="12" r="3" />
                    <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z" />
                </svg>
                Profil ayarları
            </button>
        </div>
        <div className="reader-band-row">
            <div className="reader-band-who">
                <ReaderAvatar avatarId={settings?.avatarId} achievements={achievements} size="large" />
                <div>
                    <p className="reader-band-name">{settings?.nickname ? '@' + settings.nickname : 'Kullanıcı adını seç'}</p>
                    {settings && <p className="reader-band-meta">
                        <span>{settings.isPublic ? 'Herkese açık' : 'Gizli profil'}</span>
                        {settings.nickname && settings.isPublic && <>
                            <Link to={'/okur/' + settings.nickname}>Profili gör</Link>
                            <button type="button" onClick={() => void copyLink()}>Bağlantıyı kopyala</button>
                        </>}
                    </p>}
                    {message && <p className="reader-band-message" role="status">{message}</p>}
                </div>
            </div>

            <div className="reader-band-chips">
                {profile && <Popover label="Okur kimliğin" className="band-pop band-pop-identity"
                    trigger={<span className="band-chip band-chip-identity">
                        <span className="band-chip-mark" aria-hidden="true">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 8h8M8 12h8M8 16h5" /></svg>
                        </span>
                        <span className="band-chip-text"><small>Okur kimliği</small><b>{profile.ready ? profile.title : 'Hazırlanıyor'}</b></span>
                    </span>}>
                    <TasteCardView profile={profile} own nickname={settings?.nickname} onShare={() => setSharing(true)} />
                </Popover>}
                {profile?.ready && <ShareIdentity profile={profile} nickname={settings?.nickname ?? null} open={sharing} onClose={() => setSharing(false)} />}
                {achievements.map(track => <Popover key={track.kind} label={track.label + ' rozetleri'} className="band-pop"
                    trigger={<span className="band-chip">
                        <AchievementEmblem track={track} />
                        <span className="band-chip-text"><small>{track.label}</small><b>{track.current?.title ?? emptyBadgeTitle(track, true)}</b></span>
                    </span>}>
                    <div className="band-badge-card"><AchievementCard track={track} openMilestones /></div>
                </Popover>)}
            </div>

            <div className="reader-band-side">
                <ReaderConnections bare />
            </div>
        </div>
        {settingsOpen && <div id="reader-band-settings" className="reader-band-settings"><ReaderSettings embedded /></div>}
    </section>
}
