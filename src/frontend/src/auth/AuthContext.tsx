import {
    createContext,
    useContext,
    useMemo,
    useState,
    type ReactNode,
} from 'react'

export type AuthSession = {
    userId: string
    email: string
    accessToken: string
    expiresAtUtc: string
}

type AuthContextValue = {
    session: AuthSession | null
    signIn: (session: AuthSession) => void
    signOut: () => void
}

type AuthProviderProps = {
    children: ReactNode
}

const AUTH_STORAGE_KEY = 'kitapkivo.auth'

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function getInitialSession(): AuthSession | null {
    const storedSession = sessionStorage.getItem(AUTH_STORAGE_KEY)

    if (!storedSession) {
        return null
    }

    try {
        const session = JSON.parse(storedSession) as AuthSession
        const expiresAt = Date.parse(session.expiresAtUtc)

        if (
            !session.userId ||
            !session.email ||
            !session.accessToken ||
            Number.isNaN(expiresAt) ||
            expiresAt <= Date.now()
        ) {
            sessionStorage.removeItem(AUTH_STORAGE_KEY)
            return null
        }

        return session
    } catch {
        sessionStorage.removeItem(AUTH_STORAGE_KEY)
        return null
    }
}

export function AuthProvider({ children }: AuthProviderProps) {
    const [session, setSession] = useState<AuthSession | null>(
        getInitialSession,
    )

    function signIn(newSession: AuthSession) {
        sessionStorage.setItem(
            AUTH_STORAGE_KEY,
            JSON.stringify(newSession),
        )

        setSession(newSession)
    }

    function signOut() {
        sessionStorage.removeItem(AUTH_STORAGE_KEY)
        setSession(null)
    }

    const value = useMemo(
        () => ({
            session,
            signIn,
            signOut,
        }),
        [session],
    )

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth() {
    const context = useContext(AuthContext)

    if (!context) {
        throw new Error(
            'useAuth, AuthProvider içerisinde kullanılmalıdır.',
        )
    }

    return context
}