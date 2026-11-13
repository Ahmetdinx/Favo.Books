type ReaderSettings = { nickname: string | null; isPublic: boolean; avatarId: string }
type Request = (input: string, init?: RequestInit) => Promise<Response>

export async function getReaderSettings(request: Request, signal?: AbortSignal): Promise<ReaderSettings> {
    const response = await request('/api/readers/me', { signal, cache: 'no-store' })
    if (!response.ok) throw new Error('Profil ayarların okunamadı.')
    return await response.json() as ReaderSettings
}

/** Opens the reader's profile to everyone, keeping nickname and avatar. Only called after an explicit click. */
export async function makeProfilePublic(request: Request, signal?: AbortSignal): Promise<ReaderSettings> {
    const current = await getReaderSettings(request, signal)
    if (current.isPublic) return current
    if (!current.nickname) throw new Error('Profilini açmadan önce bir kullanıcı adı seç.')
    const response = await request('/api/readers/me', {
        method: 'PUT', signal, headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ nickname: current.nickname, isPublic: true, avatarId: current.avatarId }),
    })
    if (!response.ok) {
        const body = await response.json().catch(() => ({})) as { message?: string }
        throw new Error(body.message ?? 'Profilin açılamadı. Tekrar dene.')
    }
    const saved = await response.json() as ReaderSettings
    window.dispatchEvent(new Event('reader-settings-changed'))
    return saved
}
