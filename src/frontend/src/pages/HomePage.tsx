import './ReaderHome.css'
import ReaderActivity from '../profile/ReaderSocial'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { browseBooks } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'
import type { BrowseBookItem } from '../catalog/catalogTypes'
import BookObject from '../components/BookObject'
import { useAuth } from '../auth/useAuth'
import BookRecommendations from '../community/BookRecommendations'
import LibraryRecommendations from '../community/LibraryRecommendations'
import RatingPrompts from '../community/RatingPrompts'
import OnboardingHomeCard from './OnboardingHomeCard'

function HomePage() {
    const { session, isInitializing } = useAuth()
    if (isInitializing) return <p role="status">Kitaplığın hazırlanıyor…</p>
    if (session) return <div className="reader-home">
        <OnboardingHomeCard />
        <BookRecommendations home social={<ReaderActivity />} />
        <LibraryRecommendations />
        <RatingPrompts />
    </div>
    return <WelcomeHome />
}

function WelcomeHome() {
    const [books, setBooks] = useState<BrowseBookItem[]>([])
    useEffect(() => {
        const controller = new AbortController()
        browseBooks({ query: '', searchField: 'all', category: '', page: 1, pageSize: 24, sort: 'recently-added' }, controller.signal)
            .then(result => setBooks(result.items.filter(book => book.coverImageUrl).slice(0, 8)))
            .catch(() => { /* The shelf is decorative; the page works without it. */ })
        return () => controller.abort()
    }, [])

    return (
        <section className="home-page welcome-home">
            <div className="welcome-copy">
                <h1>Son sayfası bitsin, <span>etkisi bitmesin.</span></h1>
                <p>Okuduğun romanları rafına diz, sana göre yenilerini keşfet.</p>
                <div className="welcome-actions">
                    <Link className="button primary-button" to="/register">Kitaplığını oluştur</Link>
                    <Link className="button secondary-button" to="/login">Giriş yap</Link>
                </div>
                <Link className="welcome-browse" to="/catalog">Kataloğa göz at →</Link>
            </div>

            <div className="welcome-shelf" aria-label="Katalogdan son eklenen romanlar">
                {[0, 1].map(row => <div className="welcome-shelf-row" key={row}>
                    {books.length > 0
                        ? books.slice(row * 4, row * 4 + 4).map(book => (
                            <Link key={book.bookId} to={'/catalog/books/' + book.bookId} aria-label={cleanBookTitle(book.title)} title={cleanBookTitle(book.title)}>
                                <BookObject title={book.title} coverImageUrl={book.coverImageUrl} size="sm" priority />
                            </Link>
                        ))
                        : Array.from({ length: 4 }, (_, index) => <span className="welcome-shelf-gap" key={index} aria-hidden="true" />)}
                </div>)}
            </div>
        </section>
    )
}

export default HomePage
