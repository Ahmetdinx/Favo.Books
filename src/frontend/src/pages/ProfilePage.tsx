import {
    useEffect,
    useState,
} from 'react'

import { useNavigate } from 'react-router'

import {
    useAuthenticatedFetch,
} from '../auth/useAuthenticatedFetch'

import { useAuth } from '../auth/useAuth'

type CurrentUserResponse = {
    userId: string
    email: string
}

function ProfilePage() {
    const {
        session,
        signOut,
    } = useAuth()

    const authenticatedFetch =
        useAuthenticatedFetch()

    const navigate = useNavigate()

    const [profile, setProfile] =
        useState<CurrentUserResponse | null>(
            null,
        )

    const [
        errorMessage,
        setErrorMessage,
    ] = useState('')

    const [isLoading, setIsLoading] =
        useState(true)

    const [
        requestVersion,
        setRequestVersion,
    ] = useState(0)

    const userId = session?.userId

    useEffect(() => {
        if (!userId) {
            return
        }

        const controller =
            new AbortController()

        async function loadProfile() {
            setIsLoading(true)
            setErrorMessage('')
            setProfile(null)

            try {
                const response =
                    await authenticatedFetch(
                        '/api/identity/me',
                        {
                            signal:
                                controller.signal,
                        },
                    )

                if (response.status === 401) {
                    await signOut()

                    navigate('/login', {
                        replace: true,
                        state: {
                            from: '/profile',
                        },
                    })

                    return
                }

                if (!response.ok) {
                    setErrorMessage(
                        'Profil bilgileri yüklenemedi. ' +
                        'Lütfen tekrar deneyin.',
                    )

                    return
                }

                const currentUser =
                    (await response.json()) as
                    CurrentUserResponse

                setProfile(currentUser)
            } catch {
                if (
                    !controller.signal.aborted
                ) {
                    setErrorMessage(
                        'Sunucuya ulaşılamadı. ' +
                        'API’nin çalıştığından ' +
                        'emin olun.',
                    )
                }
            } finally {
                if (
                    !controller.signal.aborted
                ) {
                    setIsLoading(false)
                }
            }
        }

        void loadProfile()

        return () => {
            controller.abort()
        }
    }, [
        authenticatedFetch,
        navigate,
        requestVersion,
        signOut,
        userId,
    ])

    if (!session) {
        return null
    }

    return (
        <section>
            <p className="eyebrow">
                Hesabım
            </p>

            <h1>Profil</h1>

            {isLoading && (
                <p role="status">
                    Profil bilgileriniz yükleniyor...
                </p>
            )}

            {errorMessage && (
                <div>
                    <p
                        className={
                            'form-message ' +
                            'error-message'
                        }
                        role="alert"
                    >
                        {errorMessage}
                    </p>

                    <button
                        className={
                            'button primary-button'
                        }
                        type="button"
                        onClick={() =>
                            setRequestVersion(
                                (current) =>
                                    current + 1,
                            )
                        }
                    >
                        Tekrar Dene
                    </button>
                </div>
            )}

            {profile && (
                <>
                    <p>
                        KitapKivo hesap bilgileriniz
                        aşağıda yer alıyor.
                    </p>

                    <dl className="profile-details">
                        <dt>E-posta</dt>
                        <dd>{profile.email}</dd>

                        <dt>
                            Kullanıcı kimliği
                        </dt>
                        <dd>{profile.userId}</dd>
                    </dl>

                    <p>
                        Okuma istatistikleri,
                        kitaplık modülü
                        hazırlandığında burada
                        gösterilecek.
                    </p>
                </>
            )}
        </section>
    )
}

export default ProfilePage