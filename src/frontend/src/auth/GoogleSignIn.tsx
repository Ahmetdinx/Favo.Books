import { identityRequest } from './sessionTransport'
import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { useAuth } from './useAuth'
import type { AuthSession } from './authTypes'
import './GoogleSignIn.css'

type GoogleSdk = { accounts: { id: {
    initialize: (options: { client_id: string; nonce: string; auto_select: boolean; callback: (response: { credential: string }) => void }) => void
    renderButton: (element: HTMLElement, options: { type: string; theme: string; size: string; text: string; locale: string }) => void
} } }
declare global { interface Window { google?: GoogleSdk } }
let sdkPromise: Promise<void> | null = null
function loadSdk() {
    if (window.google) return Promise.resolve()
    if (!sdkPromise) sdkPromise = new Promise<void>((resolve, reject) => {
        const script = document.createElement('script')
        script.src = 'https://accounts.google.com/gsi/client'
        script.async = true
        script.onload = () => resolve()
        script.onerror = () => { script.remove(); sdkPromise = null; reject(Error('Google SDK unavailable')) }
        document.head.append(script)
    })
    return sdkPromise
}

export default function GoogleSignIn() {
    const container = useRef<HTMLDivElement>(null)
    const { signIn } = useAuth()
    const navigate = useNavigate()
    const location = useLocation()
    const [enabled, setEnabled] = useState(false)
    const [busy, setBusy] = useState(false)
    const [error, setError] = useState('')
    const [version, setVersion] = useState(0)
    const from = (location.state as { from?: string } | null)?.from
    const destination = typeof from === 'string' && from.startsWith('/') && !from.startsWith('//') && !from.includes('\\') ? from : '/'
    useEffect(() => {
        const controller = new AbortController()
        let submitting = false
        setError('')
        async function prepare() {
            try {
                const response = await fetch('/api/identity/google/config', { signal: controller.signal, cache: 'no-store' })
                if (!response.ok) throw Error()
                const config = await response.json() as { enabled: boolean; clientId: string }
                if (controller.signal.aborted || !config.enabled) return
                setEnabled(true)
                await loadSdk()
                const challenge = await fetch('/api/identity/google/challenge', { method: 'POST', credentials: 'include', signal: controller.signal })
                if (!challenge.ok) throw Error()
                const { nonce } = await challenge.json() as { nonce: string }
                if (controller.signal.aborted || !container.current || !window.google) return
                window.google.accounts.id.initialize({ client_id: config.clientId, nonce, auto_select: false,
                    callback: ({ credential }) => {
                        if (submitting || controller.signal.aborted) return
                        submitting = true; setBusy(true); setError('')
                        void identityRequest('/api/identity/google', { method: 'POST', credentials: 'include', signal: controller.signal,
                            headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ credential }) })
                            .then(async result => {
                                if (!result.ok) {
                                    const data = await result.json().catch(() => ({})) as { message?: string }
                                    throw Error(data.message ?? 'Google ile giriş tamamlanamadı. Tekrar dene.')
                                }
                                const session = await result.json() as AuthSession & { needsNickname: boolean }
                                if (controller.signal.aborted) return
                                signIn(session)
                                navigate(session.needsNickname ? '/profile' : destination, { replace: true, state: { from: destination } })
                            }).catch(failure => { if (!controller.signal.aborted) setError(failure instanceof Error ? failure.message : 'Google ile giriş tamamlanamadı.') })
                            .finally(() => { if (!controller.signal.aborted) setBusy(false) })
                    } })
                container.current.replaceChildren()
                window.google.accounts.id.renderButton(container.current, { type: 'standard', theme: 'outline', size: 'large', text: 'continue_with', locale: 'tr' })
            } catch { if (!controller.signal.aborted) setError('Google girişi yüklenemedi. E-posta ile devam edebilir veya tekrar deneyebilirsin.') }
        }
        void prepare()
        return () => controller.abort()
    }, [signIn, navigate, destination, version])
    return <div className="google-sign-in" hidden={!enabled && !error} aria-busy={busy}>
        <div ref={container} hidden={busy || !!error} />
        {busy && <p role="status">Google hesabın doğrulanıyor…</p>}
        {error && <p role="alert">{error} <button type="button" onClick={() => setVersion(v => v + 1)}>Tekrar dene</button></p>}
        {enabled && <div className="auth-divider"><span>veya e-posta ile devam et</span></div>}
    </div>
}
