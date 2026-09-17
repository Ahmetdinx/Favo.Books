// A small session-only cache: memory usage does not grow with catalog size.
export function createCatalogCache<T>(capacity = 20, lifetimeMs = 60_000, now = Date.now) {
    const entries = new Map<string, { value: T; expires: number }>()
    return {
        get(key: string): T | undefined {
            const entry = entries.get(key)
            if (!entry) return undefined
            entries.delete(key)
            if (entry.expires <= now()) return undefined
            entries.set(key, entry)
            return entry.value
        },
        set(key: string, value: T) {
            entries.delete(key)
            entries.set(key, { value, expires: now() + lifetimeMs })
            while (entries.size > capacity) entries.delete(entries.keys().next().value!)
        },
    }
}
