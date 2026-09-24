import { useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router'

type ValidationProblem = {
    errors?: Record<string, string[]>
}

type ApiError = {
    message?: string
}

function RegisterPage() {
    const location = useLocation()
    const [email, setEmail] = useState('')
    const [nickname, setNickname] = useState('')
    const [password, setPassword] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [successMessage, setSuccessMessage] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        setErrorMessage('')
        setSuccessMessage('')
        setIsSubmitting(true)

        try {
            const response = await fetch('/api/identity/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    email,
                    nickname,
                    password,
                }),
            })

            if (response.status === 400) {
                const problem = (await response.json()) as ValidationProblem
                const firstError = Object.values(problem.errors ?? {})
                    .flat()
                    .at(0)

                setErrorMessage(
                    firstError ?? 'Girdiğiniz bilgileri kontrol edin.',
                )

                return
            }

            if (response.status === 409) {
                const apiError = (await response.json()) as ApiError

                setErrorMessage(
                    apiError.message ??
                    'Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.',
                )

                return
            }

            if (!response.ok) {
                setErrorMessage(
                    'Kayıt işlemi tamamlanamadı. Lütfen tekrar deneyin.',
                )

                return
            }

            setSuccessMessage(
                'Hesabınız başarıyla oluşturuldu. Artık giriş yapabilirsiniz.',
            )
            setEmail('')
            setNickname('')
            setPassword('')
        } catch {
            setErrorMessage(
                'Sunucuya ulaşılamadı. API’nin çalıştığından emin olun.',
            )
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <section className="auth-page">
            <h1>Kayıt Ol</h1>

            <p>KitapKivo hesabınızı oluşturarak okumaya başlayın.</p>

            <form onSubmit={handleSubmit}>
                <label htmlFor="register-nickname">Kullanıcı adı</label>
                <input id="register-nickname" autoComplete="username" value={nickname}
                    onChange={e => setNickname(e.target.value)} minLength={3} maxLength={24}
                    pattern="[a-zA-Z][a-zA-Z0-9_]{2,23}" disabled={isSubmitting} required aria-describedby="register-nickname-help" />
                <small id="register-nickname-help">3–24 karakter; a-z, rakam ve alt çizgi. Bir harfle başla. Profilin başlangıçta gizlidir.</small>
                <label htmlFor="register-email">E-posta</label>

                <input
                    id="register-email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    maxLength={320}
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    disabled={isSubmitting}
                    required
                />

                <label htmlFor="register-password">Parola</label>

                <input
                    id="register-password"
                    name="password"
                    type="password"
                    autoComplete="new-password"
                    minLength={8}
                    maxLength={128}
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    disabled={isSubmitting}
                    required
                />

                {errorMessage && (
                    <p className="form-message error-message" role="alert">
                        {errorMessage}
                    </p>
                )}

                {successMessage && (
                    <p className="form-message success-message" role="status">
                        {successMessage}
                    </p>
                )}

                <button
                    className="button primary-button"
                    type="submit"
                    disabled={isSubmitting}
                >
                    {isSubmitting ? 'Hesap oluşturuluyor...' : 'Hesap Oluştur'}
                </button>
            </form>
            <p><Link to="/login" state={location.state}>{successMessage ? 'Giriş yap ve devam et' : 'Zaten hesabım var'}</Link></p>
        </section>
    )
}

export default RegisterPage
