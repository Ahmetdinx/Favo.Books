import './OnboardingPage.css'
import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useAuthenticatedFetch } from '../auth/useAuthenticatedFetch'
import { useAuth } from '../auth/useAuth'
import { browseBooks, getBookById } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'
import type { BrowseBookItem } from '../catalog/catalogTypes'
import BookObject from '../components/BookObject'
import { addBookToLibrary, getLibrary, removeBookFromLibrary, ReadingApiError } from '../reading/readingApi'
import { markOnboardingSeen } from './onboardingState'
import { getReaderSettings, makeProfilePublic } from '../profile/profileVisibility'

type Genre = { slug: string; name: string }
type Preferences = { categories: string[]; criteria: string[]; availableCategories: Genre[] }
type Recommendation = { bookId: string; title: string; coverImageUrl: string; reasons: string[] }
type Step = 'genres' | 'books' | 'results'

const maxGenres = 5
const targetBooks = 5
const booksPerGenre = 12

export default function OnboardingPage() {
    const request = useAuthenticatedFetch()
    const navigate = useNavigate()
    const { session } = useAuth()
    const [step, setStep] = useState<Step>('genres')
    const [preferences, setPreferences] = useState<Preferences | null>(null)
    const [genres, setGenres] = useState<string[]>([])
    const [error, setError] = useState('')
    const [saving, setSaving] = useState(false)
    const heading = useRef<HTMLHeadingElement>(null)

    useEffect(() => {
        const controller = new AbortController()
        request('/api/community/me/reading-preferences', { signal: controller.signal, cache: 'no-store' })
            .then(async response => {
                if (!response.ok) throw new Error()
                const value = await response.json() as Preferences
                setPreferences(value)
                setGenres(value.categories)
            })
            .catch(() => { if (!controller.signal.aborted) setError('Türler yüklenemedi. Sayfayı yenileyip tekrar dene.') })
        return () => controller.abort()
    }, [request])

    useEffect(() => { heading.current?.focus() }, [step])

    function finish() {
        if (session) markOnboardingSeen(session.userId)
        navigate('/', { replace: true })
    }

    async function saveGenres(event: FormEvent) {
        event.preventDefault()
        if (!preferences) return
        setSaving(true); setError('')
        try {
            const response = await request('/api/community/me/reading-preferences', {
                method: 'PUT', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ categories: genres, criteria: preferences.criteria }),
            })
            if (!response.ok) throw new Error()
            window.dispatchEvent(new Event('reading-preferences-changed'))
            setStep('books')
        } catch { setError('Seçimin kaydedilemedi. Tekrar dene.') }
        finally { setSaving(false) }
    }

    // Memoised so the book step does not refetch on every render of this parent.
    const chosenGenres = useMemo(() => genres
        .map(slug => preferences?.availableCategories.find(genre => genre.slug === slug))
        .filter((genre): genre is Genre => !!genre), [genres, preferences])
    const stepNumber = step === 'genres' ? 1 : step === 'books' ? 2 : 3
    return <section className="onboarding" aria-labelledby="onboarding-heading">
        <header className="onboarding-top">
            <ol className="onboarding-progress" aria-label={`Adım ${stepNumber} / 3`}>
                {['Türler', 'Okudukların', 'Önerilerin'].map((label, index) =>
                    <li key={label} className={index + 1 === stepNumber ? 'current' : index + 1 < stepNumber ? 'done' : ''}
                        aria-current={index + 1 === stepNumber ? 'step' : undefined}>{label}</li>)}
            </ol>
            {step !== 'results' && <button type="button" className="onboarding-skip" onClick={finish}>Sonra yaparım</button>}
        </header>

        {error && <p className="onboarding-error" role="alert">{error}</p>}

        {step === 'genres' && <form onSubmit={event => void saveGenres(event)}>
            <h1 id="onboarding-heading" ref={heading} tabIndex={-1}>Hangi romanlar seni <em>içine çeker?</em></h1>
            <p className="onboarding-lead">En fazla {maxGenres} tür seç. Önerilerin ve bir sonraki adımdaki kitaplar buna göre gelir.</p>
            {!preferences && !error && <p role="status">Türler yükleniyor…</p>}
            {preferences && <fieldset className="onboarding-genres">
                <legend className="onboarding-visually-hidden">Roman türleri</legend>
                {preferences.availableCategories.map(genre => {
                    const checked = genres.includes(genre.slug)
                    return <label key={genre.slug} className={checked ? 'selected' : ''}>
                        <input type="checkbox" checked={checked} disabled={!checked && genres.length >= maxGenres}
                            onChange={() => setGenres(current => checked ? current.filter(slug => slug !== genre.slug) : [...current, genre.slug])} />
                        {genre.name}
                    </label>
                })}
            </fieldset>}
            <div className="onboarding-actions">
                <span className="onboarding-count">{genres.length} / {maxGenres} seçildi</span>
                <button className="button primary-button" type="submit" disabled={!preferences || saving}>
                    {saving ? 'Kaydediliyor…' : genres.length ? 'Devam et' : 'Tür seçmeden devam et'}
                </button>
            </div>
        </form>}

        {step === 'books' && preferences && <BooksStep headingRef={heading} genres={chosenGenres}
            onBack={() => setStep('genres')} onNext={() => setStep('results')} />}

        {step === 'results' && <ResultsStep headingRef={heading} onFinish={finish} />}
    </section>
}

function BooksStep({ genres, onBack, onNext, headingRef }: {
    genres: Genre[]; onBack: () => void; onNext: () => void; headingRef: React.RefObject<HTMLHeadingElement | null>
}) {
    const request = useAuthenticatedFetch()
    const [books, setBooks] = useState<BrowseBookItem[] | null>(null)
    const [query, setQuery] = useState('')
    const [results, setResults] = useState<BrowseBookItem[] | null>(null)
    const [entries, setEntries] = useState<Record<string, string>>({})
    const [pending, setPending] = useState<string[]>([])
    const [error, setError] = useState('')

    useEffect(() => {
        const controller = new AbortController()
        const lists = genres.length ? genres.map(genre => genre.slug) : ['']
        const perList = genres.length ? booksPerGenre : 30
        Promise.all([
            ...lists.map(category => browseBooks({ query: '', searchField: 'all', category, page: 1, pageSize: perList, sort: 'popular' }, controller.signal)),
            getLibrary(request, { page: 1, pageSize: 100 }, controller.signal),
        ]).then(responses => {
            const library = responses.pop() as Awaited<ReturnType<typeof getLibrary>>
            const pages = responses as Awaited<ReturnType<typeof browseBooks>>[]
            // Interleave the genres so the first rows mix every choice instead of filling up with one.
            const seen = new Set<string>()
            const merged: BrowseBookItem[] = []
            for (let index = 0; index < perList; index++) for (const page of pages) {
                const book = page.items[index]
                if (book?.coverImageUrl && !seen.has(book.bookId)) { seen.add(book.bookId); merged.push(book) }
            }
            setBooks(merged)
            setEntries(Object.fromEntries(library.items.filter(item => item.status === 'Read').map(item => [item.bookId, item.libraryEntryId])))
        }).catch(() => { if (!controller.signal.aborted) setError('Kitaplar yüklenemedi. Sayfayı yenileyip tekrar dene.') })
        return () => controller.abort()
    }, [genres, request])

    useEffect(() => {
        const text = query.trim()
        if (text.length < 2) { setResults(null); return }
        const controller = new AbortController()
        const timer = window.setTimeout(() => {
            browseBooks({ query: text, searchField: 'all', category: '', page: 1, pageSize: 12, sort: 'popular' }, controller.signal)
                .then(result => setResults(result.items))
                .catch(() => { if (!controller.signal.aborted) setResults([]) })
        }, 250)
        return () => { window.clearTimeout(timer); controller.abort() }
    }, [query])

    async function toggle(book: BrowseBookItem) {
        if (pending.includes(book.bookId)) return
        setPending(current => [...current, book.bookId]); setError('')
        const entryId = entries[book.bookId]
        try {
            if (entryId) {
                await removeBookFromLibrary(request, entryId)
                setEntries(({ [book.bookId]: _removed, ...rest }) => rest)
            } else {
                const added = await addBookToLibrary(request, { bookId: book.bookId, status: 'Read' })
                setEntries(current => ({ ...current, [book.bookId]: added.libraryEntryId }))
            }
        } catch (caught) {
            setError(caught instanceof ReadingApiError && caught.status === 409
                ? `«${cleanBookTitle(book.title)}» zaten kitaplığında başka bir rafta duruyor.`
                : 'İşlem tamamlanamadı. Tekrar dene.')
        } finally { setPending(current => current.filter(id => id !== book.bookId)) }
    }

    const count = Object.keys(entries).length
    const shown = results ?? books
    return <div>
        <h1 id="onboarding-heading" ref={headingRef} tabIndex={-1}>Hangilerini <em>okudun?</em></h1>
        <p className="onboarding-lead">Okuduklarına dokun, “Okudum” rafına eklensin. En az {targetBooks} kitap seçersen öneriler gerçekten sana göre olur.</p>
        <div className="onboarding-search">
            <label htmlFor="onboarding-search" className="onboarding-visually-hidden">Kitap ya da yazar ara</label>
            <input id="onboarding-search" type="search" placeholder="Listede yok mu? Kitap ya da yazar ara…" value={query} onChange={event => setQuery(event.target.value)} />
        </div>
        {error && <p className="onboarding-error" role="alert">{error}</p>}
        {!shown && !error && <p role="status">Kitaplar yükleniyor…</p>}
        {results && results.length === 0 && <p role="status">Bu aramayla eşleşen roman bulunamadı.</p>}
        {shown && <ul className="onboarding-books">
            {shown.map(book => {
                const selected = !!entries[book.bookId]
                const title = cleanBookTitle(book.title)
                return <li key={book.bookId}>
                    <button type="button" aria-pressed={selected} disabled={pending.includes(book.bookId)}
                        className={selected ? 'selected' : ''} onClick={() => void toggle(book)}
                        aria-label={`${title}${book.authors[0] ? ', ' + book.authors[0].name : ''}: ${selected ? 'okudum, geri al' : 'okudum olarak işaretle'}`}>
                        <BookObject title={book.title} author={book.authors[0]?.name} coverImageUrl={book.coverImageUrl} size="sm" />
                        <span className="onboarding-check" aria-hidden="true">✓</span>
                    </button>
                    <span className="onboarding-book-title">{title}</span>
                    {book.authors[0] && <span className="onboarding-book-author">{book.authors[0].name}</span>}
                </li>
            })}
        </ul>}
        <aside className="onboarding-import">
            <strong>Goodreads kullanıyor musun?</strong>
            <span>Okuduklarını oradan tek seferde aktarma yakında burada olacak.</span>
        </aside>
        <div className="onboarding-actions sticky">
            <button type="button" className="button secondary-button" onClick={onBack}>Geri</button>
            <span className="onboarding-count" role="status">{count} kitap işaretlendi{count < targetBooks ? ` · ${targetBooks - count} tane daha önerilir` : ''}</span>
            <button type="button" className="button primary-button" onClick={onNext}>{count ? 'Önerilerimi göster' : 'Atla'}</button>
        </div>
    </div>
}

function ResultsStep({ onFinish, headingRef }: { onFinish: () => void; headingRef: React.RefObject<HTMLHeadingElement | null> }) {
    const request = useAuthenticatedFetch()
    const [items, setItems] = useState<Recommendation[] | null>(null)
    const [descriptions, setDescriptions] = useState<Record<string, string>>({})
    const [error, setError] = useState('')

    useEffect(() => {
        const controller = new AbortController()
        request('/api/community/me/recommendations', { signal: controller.signal, cache: 'no-store' })
            .then(async response => {
                if (!response.ok) throw new Error()
                const value = (await response.json() as { items: Recommendation[] }).items.slice(0, 6)
                setItems(value)
                const details = await Promise.all(value.map(book => getBookById(book.bookId, controller.signal).catch(() => null)))
                setDescriptions(Object.fromEntries(details.filter(Boolean).map(book => [book!.bookId, book!.description ?? ''])))
            })
            .catch(() => { if (!controller.signal.aborted) setError('Öneriler yüklenemedi. Ana sayfada yeniden deneyebilirsin.') })
        return () => controller.abort()
    }, [request])

    const featured = useMemo(() => items ?? [], [items])
    return <div>
        <h1 id="onboarding-heading" ref={headingRef} tabIndex={-1}>Sana göre <em>{featured.length || ''} roman</em></h1>
        <p className="onboarding-lead">Seçtiklerine bakarak bunları hazırladık. Ana sayfada “İlgimi çekti” ya da “Bana uygun değil” dedikçe öneriler keskinleşir.</p>
        {error && <p className="onboarding-error" role="alert">{error}</p>}
        {!items && !error && <p role="status">Öneriler hazırlanıyor…</p>}
        {items && items.length === 0 && <p>Şimdilik öneri çıkmadı. Kataloğa göz atıp birkaç roman ekledikçe burası dolacak.</p>}
        {items && items.length > 0 && <ol className="onboarding-results">
            {items.map(book => <li key={book.bookId}>
                <Link to={`/catalog/books/${book.bookId}`} aria-label={`${cleanBookTitle(book.title)} detayları`}>
                    <BookObject title={book.title} coverImageUrl={book.coverImageUrl} size="sm" />
                </Link>
                <div>
                    <h2><Link to={`/catalog/books/${book.bookId}`}>{cleanBookTitle(book.title)}</Link></h2>
                    {book.reasons[0] && <p className="onboarding-reason">{book.reasons[0]}</p>}
                    {descriptions[book.bookId] && <p className="onboarding-description">{descriptions[book.bookId]}</p>}
                </div>
            </li>)}
        </ol>}
        <ProfileVisibilityChoice />
        <div className="onboarding-actions">
            <button type="button" className="button primary-button" onClick={onFinish}>Kitaplığıma geç</button>
        </div>
    </div>
}

// Profiles start private; following needs both sides public, so the choice is offered here explicitly.
function ProfileVisibilityChoice() {
    const request = useAuthenticatedFetch()
    const [state, setState] = useState<'loading' | 'private' | 'public' | 'saving' | 'hidden'>('loading')
    const [error, setError] = useState('')
    useEffect(() => {
        const controller = new AbortController()
        getReaderSettings(request, controller.signal)
            .then(settings => setState(settings.isPublic ? 'hidden' : 'private'))
            .catch(() => { if (!controller.signal.aborted) setState('hidden') })
        return () => controller.abort()
    }, [request])

    async function open() {
        setState('saving'); setError('')
        try { await makeProfilePublic(request); setState('public') }
        catch (caught) { setError(caught instanceof Error ? caught.message : 'Profilin açılamadı.'); setState('private') }
    }

    if (state === 'loading' || state === 'hidden') return null
    return <aside className="onboarding-visibility" aria-live="polite">
        {state === 'public'
            ? <p><strong>Profilin artık herkese açık.</strong> Diğer okurları takip edebilir, seni bulabilirler. İstediğin zaman profil ayarlarından gizleyebilirsin.</p>
            : <>
                <div>
                    <strong>Profilin şu an gizli</strong>
                    <p>Okurları takip etmek ve takip edilmek için profilinin herkese açık olması gerekir. Herkese açık profilde kullanıcı adın, kitaplığın ve puanların görünür; e-posta adresin asla görünmez.</p>
                </div>
                <button type="button" className="button secondary-button" disabled={state === 'saving'} onClick={() => void open()}>
                    {state === 'saving' ? 'Açılıyor…' : 'Profilimi herkese aç'}</button>
            </>}
        {error && <p role="alert">{error}</p>}
    </aside>
}
