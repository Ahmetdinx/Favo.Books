import { NavLink, Outlet } from 'react-router'

function AppLayout() {
  return (
    <div className="app">
      <header className="header">
        <NavLink className="logo" to="/">
          KitapKivo
        </NavLink>

        <nav className="navigation" aria-label="Ana navigasyon">
          <NavLink to="/">Ana Sayfa</NavLink>
          <NavLink to="/login">Giriş Yap</NavLink>
          <NavLink to="/register">Kayıt Ol</NavLink>
          <NavLink to="/profile">Profil</NavLink>
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