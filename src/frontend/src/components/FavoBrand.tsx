import './FavoBrand.css'

export default function FavoBrand() {
    return (
        <span className="favo-brand" role="img" aria-label="Favo Kitap">
            <img className="favo-brand-mark" src="/brand/favo-mark.png" alt="" width="40" height="40" />
            <span className="favo-brand-name" aria-hidden="true">favo</span>
            <span className="favo-brand-section" aria-hidden="true">Kitap</span>
        </span>
    )
}
