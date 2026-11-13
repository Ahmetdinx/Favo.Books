import { Link, useLocation } from 'react-router'
import { pendingInvite, rememberInvite } from '../pages/onboardingState'

/**
 * What a signed-out visitor of a shared profile sees first: an invitation to find their own reader
 * identity and their compatibility with the person who shared it. This is the loop that lets every
 * shared card bring new readers.
 */
export default function ReaderInvite({ nickname, title }: { nickname: string; title: string | null }) {
    const location = useLocation()
    // Keep the source a shared link already recorded (e.g. kart); otherwise this profile visit is the source.
    const remember = () => { if (pendingInvite()?.toLowerCase() !== nickname.toLowerCase()) rememberInvite(nickname) }
    return <aside className="reader-invite" aria-labelledby="reader-invite-heading">
        <div className="reader-invite-copy">
            <p className="reader-invite-kicker">Favo Kitap</p>
            <h2 id="reader-invite-heading">{title ? <>@{nickname} bir <em>{title.toLocaleLowerCase('tr-TR')}</em>.</> : <>@{nickname}'in kitaplığına bakıyorsun.</>} Sen hangi okursun?</h2>
            <p>Sevdiğin türleri seç, okuduğun birkaç romanı işaretle; iki dakikada okur kimliğini ve @{nickname} ile uyumunu gör.</p>
        </div>
        <div className="reader-invite-actions">
            <Link className="button primary-button" to="/register" onClick={remember}>Okur kimliğimi çıkar</Link>
            <Link className="reader-invite-login" to="/login" state={{ from: location.pathname }}
                onClick={remember}>Hesabım var, uyumumu göster</Link>
        </div>
    </aside>
}
