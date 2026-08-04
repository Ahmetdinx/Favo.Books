import { NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

function AppLayout() {
    const { session, signOut } = useAuth()
    const navigate = useNavigate()

    function handleSignOut() {
        signOut()
        navigate('/', { replace: true })
    }

    return (
        <div className="app">
            <header className="header">
                <NavLink className="logo" to="/">
                    KitapKivo
                </NavLink>

                <nav className="navigation" aria-label="Ana navigasyon">
                    <NavLink to="/">Ana Sayfa</NavLink>

                    {session ? (
                        <>
                            <span
                                className="navigation-user"
                                title={session.email}
                            >
                                {session.email}
                            </span>

                            <NavLink to="/profile">Profil</NavLink>

                            <button
                                className="logout-button"
                                type="button"
                                onClick={handleSignOut}
                            >
                                Çıkış Yap
                            </button>
                        </>
                    ) : (
                        <>
                            <NavLink to="/login">Giriş Yap</NavLink>
                            <NavLink to="/register">Kayıt Ol</NavLink>
                        </>
                    )}
                </nav>
            </header>

            <main className="main-content">
                <Outlet />
            </main>

            <footer className="footer">© 2026 KitapKivo</footer>
        </div>
    )
}

export default AppLayout