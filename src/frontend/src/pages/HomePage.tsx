import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import BookRecommendations from '../community/BookRecommendations'
import RatingPrompts from '../community/RatingPrompts'

function HomePage() {
    const { session, isInitializing } = useAuth()
    if (isInitializing) return <p role="status">Kitaplığın hazırlanıyor…</p>
    if (session) return <div><BookRecommendations home /><RatingPrompts /></div>
    return (
        <section className="home-page">
            <div className="hero-copy">
                <p className="eyebrow">Kişisel dijital kütüphanen</p>

                <h1>
                    Her kitap, sende başka bir
                    <span className="accent-text"> iz bırakır.</span>
                </h1>

                <p className="hero-description">
                    Okuduklarını kaydet, kendi kitaplığını oluştur ve sıradaki
                    kitabını keşfet. Bütün okuma yolculuğun tek bir rafta.
                </p>

                <div className="page-actions">
                    <Link className="button primary-button" to="/register">
                        Kütüphaneni Oluştur
                    </Link>

                    <Link className="button secondary-button" to="/login">
                        Giriş Yap
                    </Link>
                </div>

                <div className="reading-features" aria-label="KitapKivo özellikleri">
                    <span>Okuma listeleri</span>
                    <span>Puan ve yorumlar</span>
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
                        <span>Roman</span>
                    </div>

                    <div className="book book-cream">
                        <span>Şiir</span>
                    </div>

                    <div className="book book-navy">
                        <span>Tarih</span>
                    </div>

                    <div className="book book-mustard">
                        <span>Felsefe</span>
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
