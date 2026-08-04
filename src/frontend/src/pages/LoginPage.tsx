function LoginPage() {
  return (
    <section className="auth-page">
      <h1>Giriş Yap</h1>
      <p>KitapKivo hesabınıza giriş yapın.</p>

      <form onSubmit={(event) => event.preventDefault()}>
        <label htmlFor="login-email">E-posta</label>

        <input
          id="login-email"
          name="email"
          type="email"
          autoComplete="email"
          required
        />

        <label htmlFor="login-password">Parola</label>

        <input
          id="login-password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
        />

        <button className="button primary-button" type="submit">
          Giriş Yap
        </button>
      </form>
    </section>
  )
}

export default LoginPage