import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import BookRecommendations from '../community/BookRecommendations'
import LibraryRecommendations from '../community/LibraryRecommendations'
import RatingPrompts from '../community/RatingPrompts'

function HomePage() {
    const { session, isInitializing } = useAuth()
    if (isInitializing) return <p role="status">Kitaplığın hazırlanıyor…</p>
    if (session) return <div><BookRecommendations home /><LibraryRecommendations /><RatingPrompts /></div>
    return (
        <section className="home-page">
            <div className="hero-copy">
                <p className="eyebrow">Bir sonraki favori romanın burada</p>

                <h1>
                    Son sayfası bitsin,
                    <span className="accent-text"> etkisi bitmesin.</span>
                </h1>

                <p className="hero-description">
                    Sevdiğin romanları ekle, zevkine göre yeni hikâyeler keşfet.
                    Bir sonraki başucu kitabını birlikte bulalım.
                </p>

                <div className="page-actions">
                    <Link className="button primary-button" to="/register">
                        Kitaplığını oluştur
                    </Link>

                    <Link className="button secondary-button" to="/login">
                        Giriş yap
                    </Link>
                </div>

                <div className="reading-features" aria-label="KitapKivo özellikleri">
                    <span>Okuma listeleri</span>
                    <span>Sana özel öneriler</span>
                    <span>Kişisel kitap rafı</span>
                </div>
            </div>

            <aside
                className="library-showcase"
                aria-label="KitapKivo kitaplığı ön izlemesi"
            >
                <div className="showcase-header">
                    <p>KitapKivo Kütüphanesi</p>
                    <span>Raf 01</span>
                </div>

                <div className="bookshelf" aria-hidden="true">
                    <div className="book book-burgundy">
                        <span>Klasikler</span>
                    </div>

                    <div className="book book-forest">
                        <span>Fantastik</span>
                    </div>

                    <div className="book book-cream">
                        <span>Romantik</span>
                    </div>

                    <div className="book book-navy">
                        <span>Polisiye</span>
                    </div>

                    <div className="book book-mustard">
                        <span>Bilim kurgu</span>
                    </div>
                </div>

                <div className="shelf-line" />

                <div className="showcase-note">
                    <span>Okuma yolculuğun</span>
                    <strong>Okudukça büyüyen, sana ait bir dünya.</strong>
                </div>
            </aside>
        </section>
    )
}

export default HomePage
