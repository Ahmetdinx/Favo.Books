import { useState } from 'react'
import { Link } from 'react-router'
import type { TasteProfile } from './tasteCardImage'
import ShareIdentity from './ShareIdentity'
import './TasteCard.css'
import { useTasteProfile } from './useTasteProfile'

type Props = {
    /** Signed-in reader's own card (true) or a public reader's card. */
    own: boolean
    nickname?: string | null
}

const spineColors = ['#7a3b2e', '#3f5a4a', '#b0813f']

/** The library-card view of a reader's taste: leather header, genre spines, strengths and stamps. */
/** onShare: open the share sheet from the parent instead (needed when the card sits in a popover that closes on outside clicks). */
export function TasteCardView({ profile, own, nickname, onShare }: { profile: TasteProfile; own: boolean; nickname?: string | null; onShare?: () => void }) {
    const [sharing, setSharing] = useState(false)

    if (!profile.ready) return <article className="taste-card taste-card-pending" aria-labelledby="taste-card-heading">
        <header className="taste-card-cover">
            <p className="taste-card-kicker">Okur kimliğin</p>
            <h2 id="taste-card-heading">Hazırlanıyor</h2>
        </header>
        <div className="taste-card-body">
            <p>{profile.ratingsNeeded} değerlendirme ya da {profile.booksNeeded} okunmuş roman daha; kartın oluşsun.</p>
            <Link to="/catalog">Kataloğa git</Link>
        </div>
    </article>


    return <article className="taste-card" aria-labelledby="taste-card-heading">
        <header className="taste-card-cover">
            <div className="taste-card-head">
                <p className="taste-card-kicker">{own ? 'Okur kimliğin' : 'Okur kimliği'}</p>
                <span className="taste-card-number" aria-hidden="true">№ {String(profile.readCount + profile.ratingCount).padStart(3, '0')}</span>
            </div>
            <h2 id="taste-card-heading">{profile.title}</h2>
            {nickname && <p className="taste-card-owner">@{nickname}</p>}
        </header>
        <div className="taste-card-body">
            {profile.genres.length > 0 && <>
                <div className="taste-spines" aria-hidden="true">
                    {profile.genres.map((genre, i) => <span key={genre.slug}
                        style={{ flexGrow: Math.max(genre.percent, 6), backgroundColor: spineColors[i % spineColors.length] }} />)}
                </div>
                <ul className="taste-legend" aria-label="En çok okunan türler">
                    {profile.genres.map((genre, i) => <li key={genre.slug}>
                        <i style={{ background: spineColors[i % spineColors.length] }} aria-hidden="true" />
                        <span>{genre.name}</span><strong>%{genre.percent}</strong>
                    </li>)}
                </ul>
            </>}
            {own && profile.strengths.length === 0 && profile.ratingsNeeded > 0 && <p className="taste-hint">
                {profile.ratingsNeeded} değerlendirme daha yaparsan, sevdiğin romanlarda nelerin öne çıktığı da burada görünür.
            </p>}
            {profile.strengths.length > 0 && <p className="taste-strengths">
                {own ? 'Sevdiğin' : 'Sevdiği'} romanlarda öne çıkan <strong>{profile.strengths.map(s => s.label).join(' ve ')}</strong>
            </p>}
            {profile.traits.length > 0 && <ul className="taste-traits" aria-label="Okur özellikleri">
                {profile.traits.map(trait => <li key={trait}>{trait}</li>)}
            </ul>}
            <footer className="taste-card-foot">
                <p className="taste-stats">{profile.readCount} okunmuş · {profile.ratingCount} değerlendirme
                    {profile.averageScore != null && <> · ort. {profile.averageScore.toLocaleString('tr-TR', { minimumFractionDigits: 1 })}</>}</p>
                {own && <button type="button" onClick={() => onShare ? onShare() : setSharing(true)}>Kimliğini paylaş</button>}
            </footer>
            {own && !onShare && <ShareIdentity profile={profile} nickname={nickname ?? null} open={sharing} onClose={() => setSharing(false)} />}
        </div>
    </article>
}

/** Inline card, used on public reader profiles. */
export default function TasteCard({ own, nickname }: Props) {
    const { profile, failed } = useTasteProfile(own, nickname)
    if (failed || !profile || (!profile.ready && !own)) return null
    return <div className="taste-card-inline"><TasteCardView profile={profile} own={own} nickname={nickname} /></div>
}
