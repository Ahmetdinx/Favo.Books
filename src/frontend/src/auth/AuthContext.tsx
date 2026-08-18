import {
    useCallback,
    useEffect,
    useMemo,
    useRef,
    useState,
    type ReactNode,
} from 'react'

import {
    AuthContext,
} from './AuthContextDefinition'

import type {
    AuthSession,
} from './authTypes'

type AuthProviderProps = {
    children: ReactNode
}

export function AuthProvider({
    children,
}: AuthProviderProps) {
    const [session, setSession] =
        useState<AuthSession | null>(null)

    const sessionRef =
        useRef<AuthSession | null>(null)

    const [isInitializing, setIsInitializing] =
        useState(true)

    const refreshPromiseRef =
        useRef<Promise<AuthSession | null> | null>(
            null,
        )

    const updateSession = useCallback(
        (newSession: AuthSession | null) => {
            sessionRef.current = newSession
            setSession(newSession)
        },
        [],
    )

    const getSession = useCallback(() => {
        return sessionRef.current
    }, [])

    const signIn = useCallback(
        (newSession: AuthSession) => {
            updateSession(newSession)
        },
        [updateSession],
    )

    const refreshSession = useCallback(async () => {
        if (refreshPromiseRef.current) {
            return refreshPromiseRef.current
        }

        const refreshRequest = (async () => {
            try {
                const response = await fetch(
                    '/api/identity/refresh',
                    {
                        method: 'POST',
                        credentials: 'include',
                    },
                )

                if (!response.ok) {
                    updateSession(null)
                    return null
                }

                const refreshedSession =
                    (await response.json()) as AuthSession

                updateSession(refreshedSession)

                return refreshedSession
            } catch {
                updateSession(null)
                return null
            }
        })()

        refreshPromiseRef.current =
            refreshRequest

        try {
            return await refreshRequest
        } finally {
            if (
                refreshPromiseRef.current ===
                refreshRequest
            ) {
                refreshPromiseRef.current = null
            }
        }
    }, [updateSession])

    const signOut = useCallback(async () => {
        try {
            await fetch('/api/identity/logout', {
                method: 'POST',
                credentials: 'include',
            })
        } catch {
            // API erişilemese bile yerel oturum kapatılır.
        } finally {
            updateSession(null)
        }
    }, [updateSession])

    useEffect(() => {
        let isActive = true

        async function initializeSession() {
            await refreshSession()

            if (isActive) {
                setIsInitializing(false)
            }
        }

        void initializeSession()

        return () => {
            isActive = false
        }
    }, [refreshSession])

    const value = useMemo(
        () => ({
            session,
            isInitializing,
            signIn,
            signOut,
            refreshSession,
            getSession,
        }),
        [
            session,
            isInitializing,
            signIn,
            signOut,
            refreshSession,
            getSession,
        ],
    )

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    )
}