import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { AuthContext } from './AuthContextDefinition'
import type { AuthSession } from './authTypes'
import { identityRequest, publishSessionEvent, subscribeSessionEvents } from './sessionTransport'

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<AuthSession | null>(null)
    const sessionRef = useRef<AuthSession | null>(null)
    const [isInitializing, setIsInitializing] = useState(true)
    const pending = useRef<Promise<AuthSession | null> | null>(null)
    const epoch = useRef(0)
    const updateSession = useCallback((value: AuthSession | null) => {
        sessionRef.current = value
        setSession(value)
    }, [])
    const getSession = useCallback(() => sessionRef.current, [])
    const signIn = useCallback((value: AuthSession) => {
        epoch.current++
        updateSession(value)
        publishSessionEvent('login')
    }, [updateSession])
    const refreshSession = useCallback(() => {
        if (pending.current) return pending.current
        const current = epoch.current
        const request = (async () => {
            try {
                const response = await identityRequest('/api/identity/refresh', { method: 'POST' })
                const value = response.ok ? await response.json() as AuthSession : null
                if (epoch.current !== current) return sessionRef.current
                updateSession(value)
                return value
            } catch {
                if (epoch.current === current) updateSession(null)
                return null
            } finally { pending.current = null; setIsInitializing(false) }
        })()
        pending.current = request
        return request
    }, [updateSession])
    const signOut = useCallback(async () => {
        const response = await identityRequest('/api/identity/logout', { method: 'POST' })
        if (!response.ok) throw Error('Çıkış tamamlanamadı. Bağlantını kontrol edip tekrar dene.')
        epoch.current++
        updateSession(null)
        publishSessionEvent('logout')
    }, [updateSession])
    useEffect(() => {
        void refreshSession()
        const unsubscribe = subscribeSessionEvents(action => {
            epoch.current++
            updateSession(null)
            if (action === 'login') void (pending.current ?? Promise.resolve()).then(refreshSession)
        })
        const onPageShow = (event: PageTransitionEvent) => { if (event.persisted) void refreshSession() }
        window.addEventListener('pageshow', onPageShow)
        return () => { unsubscribe(); window.removeEventListener('pageshow', onPageShow) }
    }, [refreshSession, updateSession])
    const value = useMemo(() => ({ session, isInitializing, signIn, signOut, refreshSession, getSession }),
        [session, isInitializing, signIn, signOut, refreshSession, getSession])
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
