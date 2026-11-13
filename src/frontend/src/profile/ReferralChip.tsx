import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import Popover from './Popover'
import ReaderAvatar from './ReaderAvatar'

type Referrals = { total: number; readers: { nickname: string; avatarId: string }[] }

/** "Readers who came through you": counts sign-ups from this reader's shared cards and links. */
export default function ReferralChip({ onShare }: { onShare?: () => void }) {
    const request = useAuthenticatedFetch()
    const [data, setData] = useState<Referrals | null>(null)
    useEffect(() => {
        const controller = new AbortController()
        request('/api/readers/me/referrals', { signal: controller.signal, cache: 'no-store' })
            .then(async response => { if (response.ok) setData(await response.json() as Referrals) })
            .catch(() => { /* optional */ })
        return () => controller.abort()
    }, [request])
    if (!data) return null
    return <Popover label="Seninle gelen okurlar" className="band-pop"
        trigger={<span className="band-chip">
            <span className="band-chip-mark" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M4 19c1.5-4 4-5.5 6-5.5s4.5 1.5 6 5.5" /><circle cx="10" cy="8" r="3.2" /><path d="M16.5 5.5v5M14 8h5" /></svg>
            </span>
            <span className="band-chip-text"><small>Davetlerin</small><b>{data.total ? `${data.total} okur` : 'Henüz yok'}</b></span>
        </span>}>
        <div className="referral-card">
            <h3>{data.total ? `${data.total} okur seninle geldi` : 'Seninle gelen okurlar'}</h3>
            <p>{data.total
                ? 'Paylaştığın kart ya da linkten Favo\'ya katılan okurlar. Gizli profiller yalnızca sayıya dahil.'
                : 'Okur kimliğini ya da profil linkini paylaş; senin paylaşımınla katılan okurlar burada görünür.'}</p>
            {data.readers.length > 0 && <ul>{data.readers.map(reader => <li key={reader.nickname}>
                <Link to={'/okur/' + encodeURIComponent(reader.nickname)}><ReaderAvatar avatarId={reader.avatarId} size="small" /><span>@{reader.nickname}</span></Link>
            </li>)}</ul>}
            {onShare && <button type="button" className="referral-share" onClick={onShare}>Kimliğini paylaş</button>}
        </div>
    </Popover>
}
