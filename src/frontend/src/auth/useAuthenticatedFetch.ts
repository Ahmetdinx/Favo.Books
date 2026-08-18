import { useCallback } from 'react'

import { useAuth } from './useAuth'

import type {
    AuthSession,
} from './authTypes'

const EXPIRATION_BUFFER_MILLISECONDS = 5_000

function shouldRefreshSession(
    session: AuthSession,
) {
    const expiresAt =
        Date.parse(session.expiresAtUtc)

    return (
        Number.isNaN(expiresAt) ||
        expiresAt <=
        Date.now() +
        EXPIRATION_BUFFER_MILLISECONDS
    )
}

function sendAuthenticatedRequest(
    input: string | URL,
    init: RequestInit,
    accessToken: string,
) {
    const headers =
        new Headers(init.headers)

    headers.set(
        'Authorization',
        `Bearer ${accessToken}`,
    )

    return fetch(input, {
        ...init,
        headers,
        credentials: 'include',
    })
}

function createUnauthorizedResponse() {
    return new Response(null, {
        status: 401,
        statusText: 'Unauthorized',
    })
}

export function useAuthenticatedFetch() {
    const {
        getSession,
        refreshSession,
    } = useAuth()

    return useCallback(
        async (
            input: string | URL,
            init: RequestInit = {},
        ): Promise<Response> => {
            let currentSession =
                getSession()

            if (
                !currentSession ||
                shouldRefreshSession(
                    currentSession,
                )
            ) {
                currentSession =
                    await refreshSession()

                if (!currentSession) {
                    return createUnauthorizedResponse()
                }
            }

            const usedAccessToken =
                currentSession.accessToken

            let response =
                await sendAuthenticatedRequest(
                    input,
                    init,
                    usedAccessToken,
                )

            if (response.status !== 401) {
                return response
            }

            const latestSession =
                getSession()

            if (
                latestSession &&
                latestSession.accessToken !==
                usedAccessToken
            ) {
                return sendAuthenticatedRequest(
                    input,
                    init,
                    latestSession.accessToken,
                )
            }

            const refreshedSession =
                await refreshSession()

            if (!refreshedSession) {
                return response
            }

            response =
                await sendAuthenticatedRequest(
                    input,
                    init,
                    refreshedSession.accessToken,
                )

            return response
        },
        [
            getSession,
            refreshSession,
        ],
    )
}