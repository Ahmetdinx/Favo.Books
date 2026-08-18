export type AuthSession = {
    userId: string
    email: string
    accessToken: string
    expiresAtUtc: string
}

export type AuthContextValue = {
    session: AuthSession | null
    isInitializing: boolean
    signIn: (session: AuthSession) => void
    signOut: () => Promise<void>
    refreshSession: () => Promise<AuthSession | null>
    getSession: () => AuthSession | null
}