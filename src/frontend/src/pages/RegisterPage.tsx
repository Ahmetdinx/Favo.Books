function RegisterPage() {
    return (
        <section className="auth-page">
            <h1>Kayıt Ol</h1>
            <p>KitapKivo hesabınızı oluşturarak okumaya başlayın.</p>

            <form onSubmit={(event) => event.preventDefault()}>
                <label htmlFor="register-email">E-posta</label>

                <input
                    id="register-email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                />

                <label htmlFor="register-password">Parola</label>

                <input
                    id="register-password"
                    name="password"
                    type="password"
                    autoComplete="new-password"
                    required
                />

                <button className="button primary-button" type="submit">
                    Hesap Oluştur
                </button>
            </form>
        </section>
    )
}

export default RegisterPage