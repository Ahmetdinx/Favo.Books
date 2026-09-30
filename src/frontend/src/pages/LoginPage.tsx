import { identityRequest } from '../auth/sessionTransport'
import GoogleSignIn from '../auth/GoogleSignIn'
import FavoBrand from '../components/FavoBrand'
import {
    useState,
    type FormEvent,
} from 'react'

import {
    Link,
    useLocation,
    useNavigate,
} from 'react-router'

import { useAuth } from '../auth/useAuth'

import type {
    AuthSession,
} from '../auth/authTypes'

type ValidationProblem = {
    errors?: Record<string, string[]>
}

function LoginPage() {
    const { signIn } = useAuth()
    const location = useLocation()
    const navigate = useNavigate()

    const [email, setEmail] = useState('')
    const [password, setPassword] =
        useState('')

    const [
        errorMessage,
        setErrorMessage,
    ] = useState('')

    const [
        successMessage,
        setSuccessMessage,
    ] = useState('')

    const [
        isSubmitting,
        setIsSubmitting,
    ] = useState(false)

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>,
    ) {
        event.preventDefault()

        setErrorMessage('')
        setSuccessMessage('')
        setIsSubmitting(true)

        try {
            const response = await identityRequest(
                '/api/identity/login',
                {
                    method: 'POST',
                    credentials: 'include',
                    headers: {
                        'Content-Type':
                            'application/json',
                    },
                    body: JSON.stringify({
                        email: email.trim(),
                        password,
                    }),
                },
            )

            if (response.status === 400) {
                const problem =
                    (await response.json()) as
                    ValidationProblem

                const firstError =
                    Object.values(
                        problem.errors ?? {},
                    )
                        .flat()
                        .at(0)

                setErrorMessage(
                    firstError ??
                    'Girdiğiniz bilgileri kontrol edin.',
                )

                return
            }

            if (response.status === 401) {
                setErrorMessage(
                    'E-posta adresi veya parola hatalı.',
                )

                return
            }

            if (!response.ok) {
                setErrorMessage(
                    'Giriş işlemi tamamlanamadı. ' +
                    'Lütfen tekrar deneyin.',
                )

                return
            }

            const loginResponse =
                (await response.json()) as
                AuthSession

            signIn(loginResponse)

            const requestedPath = (
                location.state as {
                    from?: string
                } | null
            )?.from

            const destination =
                requestedPath?.startsWith('/') &&
                    !requestedPath.startsWith('//') &&
                    !requestedPath.includes('\\') &&
                    requestedPath !== '/login'
                    ? requestedPath
                    : '/profile'

            navigate(destination, {
                replace: true,
            })
        } catch {
            setErrorMessage(
                'Sunucuya ulaşılamadı. ' +
                'API’nin çalıştığından emin olun.',
            )
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <section className="auth-page">
            <FavoBrand />
            <h1>Giriş yap</h1>
            <GoogleSignIn />

            <p>
                Favo Kitap hesabınıza giriş yapın.
            </p>

            <form onSubmit={handleSubmit}>
                <label htmlFor="login-email">
                    E-posta
                </label>

                <input
                    id="login-email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    maxLength={320}
                    value={email}
                    onChange={(event) =>
                        setEmail(
                            event.target.value,
                        )
                    }
                    disabled={isSubmitting}
                    required
                />

                <label htmlFor="login-password">
                    Parola
                </label>

                <input
                    id="login-password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    maxLength={128}
                    value={password}
                    onChange={(event) =>
                        setPassword(
                            event.target.value,
                        )
                    }
                    disabled={isSubmitting}
                    required
                />

                {errorMessage && (
                    <p
                        className={
                            'form-message ' +
                            'error-message'
                        }
                        role="alert"
                    >
                        {errorMessage}
                    </p>
                )}

                {successMessage && (
                    <p
                        className={
                            'form-message ' +
                            'success-message'
                        }
                        role="status"
                    >
                        {successMessage}
                    </p>
                )}

                <button
                    className={
                        'button primary-button'
                    }
                    type="submit"
                    disabled={isSubmitting}
                >
                    {isSubmitting
                        ? 'Giriş yapılıyor...'
                        : 'Giriş yap'}
                </button>
            </form>
            <p>Hesabın yok mu? <Link to="/register" state={location.state}>Kayıt ol</Link></p>
        </section>
    )
}

export default LoginPage
