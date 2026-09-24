import { useEffect, useState } from 'react'

import {
    NavLink,
    Outlet,
    useNavigate,
    useLocation,
} from 'react-router'

import { useAuth } from '../auth/useAuth'
import ReaderSearch from './ReaderSearch'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import '../profile/Readers.css'

function AppLayout() {
    const {
        session,
        isInitializing,
        signOut,
    } = useAuth()

    const navigate = useNavigate()
    const location = useLocation()
    const authFetch = useAuthenticatedFetch()
    const [reader, setReader] = useState<{ nickname: string | null } | null>(null)
    const [readerVersion, setReaderVersion] = useState(0)
    useEffect(() => {
        const update = () => setReaderVersion(v => v + 1)
        window.addEventListener('reader-settings-changed', update)
        return () => window.removeEventListener('reader-settings-changed', update)
    }, [])
    useEffect(() => {
        setReader(null)
        if (!session?.userId) return
        const controller = new AbortController()
        authFetch('/api/readers/me', { signal: controller.signal }).then(async response => {
            if (response.ok) {
                const value = await response.json()
                if (!controller.signal.aborted) setReader(value)
            }
        }).catch(() => {})
        return () => controller.abort()
    }, [session?.userId, authFetch, readerVersion])
    useEffect(() => {
        window.scrollTo({ top: 0, behavior: 'instant' })
    }, [location.pathname])

    const [
        isSigningOut,
        setIsSigningOut,
    ] = useState(false)

    async function handleSignOut() {
        setIsSigningOut(true)

        try {
            await signOut()

            navigate('/', {
                replace: true,
            })
        } finally {
            setIsSigningOut(false)
        }
    }

    return (
        <div className="app">
            <header className="header">
                <NavLink
                    className="logo"
                    to="/"
                >
                    KitapKivo
                </NavLink>
                <ReaderSearch key={location.pathname} />

                <nav
                    className="navigation"
                    aria-label="Ana navigasyon"
                >
                    <NavLink to="/">
                        Ana Sayfa
                    </NavLink>

                    <NavLink to="/catalog">
                        Katalog
                    </NavLink>

                    {isInitializing ? (
                        <span className="navigation-user">
                            Oturum kontrol ediliyor...
                        </span>
                    ) : session ? (
                        <>
                            <span
                                className="navigation-user"
                                title={reader?.nickname ?? 'Okur'}
                            >
                                {reader?.nickname ? '@' + reader.nickname : ''}
                            </span>

                            <NavLink to="/profile">
                                Profil
                            </NavLink>

                            <button
                                className="logout-button"
                                type="button"
                                onClick={
                                    handleSignOut
                                }
                                disabled={
                                    isSigningOut
                                }
                            >
                                {isSigningOut
                                    ? 'Çıkış yapılıyor...'
                                    : 'Çıkış Yap'}
                            </button>
                        </>
                    ) : (
                        <>
                            <NavLink to="/login">
                                Giriş Yap
                            </NavLink>

                            <NavLink to="/register">
                                Kayıt Ol
                            </NavLink>
                        </>
                    )}
                </nav>
            </header>

            <main className="main-content library-page-turn" key={location.pathname}>
                {session && reader && !reader.nickname && location.pathname !== '/profile' &&
                    <div className="reader-onboarding">Kitaplığın hazır. <NavLink to="/profile">Kullanıcı adını seç →</NavLink></div>}
                <Outlet />
            </main>

            <footer className="footer">
                © 2026 KitapKivo
            </footer>
        </div>
    )
}

export default AppLayout
