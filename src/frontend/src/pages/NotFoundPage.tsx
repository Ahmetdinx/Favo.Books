import { Link } from 'react-router'

function NotFoundPage() {
  return (
    <section>
      <p className="eyebrow">404</p>
      <h1>Sayfa bulunamadı</h1>

      <p>Aradığınız sayfa kaldırılmış veya hiç var olmamış olabilir.</p>

      <Link className="button primary-button" to="/">
        Ana Sayfaya Dön
      </Link>
    </section>
  )
}

export default NotFoundPage