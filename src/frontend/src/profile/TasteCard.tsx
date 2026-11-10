import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { downloadTasteCard, type TasteProfile } from './tasteCardImage'
import './TasteCard.css'

type Props = {
    /** Signed-in reader's own card (true) or a public reader's card. */
    own: boolean
    nickname?: string | null
}

export default function TasteCard({ own, nickname }: Props) {
    const request = useAuthenticatedFetch()
    const [profile, setProfile] = useState<TasteProfile | null>(null)
    const [failed, setFailed] = useState(false)
    const [sharing, setSharing] = useState(false)
    const [message, setMessage] = useState('')
    const [ownNickname, setOwnNickname] = useState<string | null>(null)
    const url = own ? '/api/readers/me/taste' : nickname ? '/api/readers/' + encodeURIComponent(nickname) + '/taste' : null

    useEffect(() => {
        if (!url) return
        const controller = new AbortController()
        setProfile(null); setFailed(false)
        const load = own ? request(url, { signal: controller.signal, cache: 'no-store' }) : fetch(url, { signal: controller.signal, cache: 'no-store' })
        void load.then(async response => {
            if (!response.ok) throw new Error('taste unavailable')
            const data = await response.json() as TasteProfile
            if (!controller.signal.aborted) setProfile(data)
        }).catch(() => { if (!controller.signal.aborted) setFailed(true) })
        return () => controller.abort()
    }, [own, request, url])

    useEffect(() => {
        if (!own) return
        const controller = new AbortController()
        void request('/api/readers/me', { signal: controller.signal, cache: 'no-store' })
            .then(r => r.ok ? r.json() as Promise<{ nickname: string | null }> : null)
            .then(me => { if (me && !controller.signal.aborted) setOwnNickname(me.nickname) })
            .catch(() => { /* the card works without a nickname */ })
        return () => controller.abort()
    }, [own, request])

    if (failed || !profile) return null

    if (!profile.ready) {
        if (!own) return null
        const books = profile.booksNeeded, ratings = profile.ratingsNeeded
        return <section className="taste-card taste-card-pending" aria-labelledby="taste-card-heading">
            <p className="taste-card-kicker">Okur kimliğin</p>
            <h2 id="taste-card-heading">Hazırlanıyor</h2>
            <p>{ratings} değerlendirme ya da {books} okunmuş roman daha; kartın oluşsun.</p>
            <Link to="/catalog">Kataloğa git</Link>
        </section>
    }

    async function share() {
        if (!profile) return
        setSharing(true); setMessage('')
        try {
            const result = await downloadTasteCard(profile, nickname ?? ownNickname)
            setMessage(result === 'shared' ? 'Kart paylaşıldı.' : 'Kart indirildi.')
        } catch { setMessage('Kart hazırlanamadı.') }
        finally { setSharing(false) }
    }

    return <section className="taste-card" aria-labelledby="taste-card-heading">
        <div className="taste-card-head">
            <p className="taste-card-kicker">{own ? 'Okur kimliğin' : 'Okur kimliği'}</p>
            <span className="taste-card-number" aria-hidden="true">№ {String(profile.readCount + profile.ratingCount).padStart(3, '0')}</span>
        </div>
        <h2 id="taste-card-heading">{profile.title}</h2>
        {profile.genres.length > 0 && <ul className="taste-genres" aria-label="En çok okunan türler">
            {profile.genres.map(genre => <li key={genre.slug}>
                <span>{genre.name}</span>
                <span className="taste-genre-bar" aria-hidden="true"><span style={{ width: `${genre.percent}%` }} /></span>
                <strong>%{genre.percent}</strong>
            </li>)}
        </ul>}
        {own && profile.strengths.length === 0 && profile.ratingsNeeded > 0 && <p className="taste-hint">
            {profile.ratingsNeeded} değerlendirme daha yaparsan, sevdiğin romanlarda nelerin öne çıktığı da burada görünür.
        </p>}
        {profile.strengths.length > 0 && <p className="taste-strengths">
            {own ? 'Sevdiğin' : 'Sevdiği'} romanlarda öne çıkan: <strong>{profile.strengths.map(s => s.label).join(' ve ')}</strong>
        </p>}
        <div className="taste-card-foot">
            {profile.traits.length > 0 && <ul className="taste-traits" aria-label="Okur özellikleri">
                {profile.traits.map(trait => <li key={trait}>{trait}</li>)}
            </ul>}
            <p className="taste-stats">{profile.readCount} okunmuş · {profile.ratingCount} değerlendirme
                {profile.averageScore != null && <> · ort. {profile.averageScore.toLocaleString('tr-TR', { minimumFractionDigits: 1 })}</>}</p>
        </div>
        {own && <div className="taste-card-actions">
            <button type="button" onClick={() => void share()} disabled={sharing}>{sharing ? 'Hazırlanıyor…' : 'Kartı paylaş'}</button>
            {message && <span role="status">{message}</span>}
        </div>}
    </section>
}
