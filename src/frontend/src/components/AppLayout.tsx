import { useState } from 'react'

import {
    NavLink,
    Outlet,
    useNavigate,
} from 'react-router'

import { useAuth } from '../auth/useAuth'

function AppLayout() {
    const {
        session,
        isInitializing,
        signOut,
    } = useAuth()

    const navigate = useNavigate()

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
                                title={session.email}
                            >
                                {session.email}
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

            <main className="main-content">
                <Outlet />
            </main>

            <footer className="footer">
                © 2026 KitapKivo
            </footer>
        </div>
    )
}

export default AppLayout