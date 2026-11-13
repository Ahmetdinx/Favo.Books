import './OnboardingPage.css'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { getLibrarySummary } from '../reading/readingApi'
import { hasSeenOnboarding, markOnboardingSeen } from './onboardingState'

// Invites readers with an almost empty library (e.g. Google sign-ups, or anyone who skipped) to the guide.
export default function OnboardingHomeCard() {
    const { session } = useAuth()
    const request = useAuthenticatedFetch()
    const userId = session?.userId
    const [visible, setVisible] = useState(false)

    useEffect(() => {
        if (!userId || hasSeenOnboarding(userId)) return
        const controller = new AbortController()
        getLibrarySummary(request, controller.signal)
            .then(summary => { if (summary.totalCount < 5) setVisible(true) })
            .catch(() => { /* The card is optional; the home page works without it. */ })
        return () => controller.abort()
    }, [userId, request])

    if (!visible || !userId) return null
    return <aside className="onboarding-home-card" aria-labelledby="onboarding-card-heading">
        <div>
            <h2 id="onboarding-card-heading">Zevkini tanıyalım</h2>
            <p>Sevdiğin türleri seç, okuduğun birkaç romanı işaretle; önerilerin iki dakikada sana göre olsun.</p>
        </div>
        <Link className="button primary-button" to="/baslangic">Başlayalım</Link>
        <button type="button" className="onboarding-skip" onClick={() => { markOnboardingSeen(userId); setVisible(false) }}>Gizle</button>
    </aside>
}
