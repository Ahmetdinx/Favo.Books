import { Link, useSearchParams } from 'react-router'
import type { CatalogLibrary } from './useCatalogLibrary'
import type { LibraryEntryStatus } from './readingTypes'
import './CatalogLibraryActions.css'

const labels: Record<LibraryEntryStatus, string> = {
    WantToRead: 'Okumak istiyorum', Reading: 'Okuyorum', Read: 'Okudum', OnHold: 'Ara verdim', Dropped: 'Yarım bıraktım',
}
const choices = ['WantToRead', 'Reading', 'Read'] as const
export default function CatalogLibraryActions({ bookId, title, library }: { bookId: string; title: string; library: CatalogLibrary }) {
    const [params] = useSearchParams()
    const entry = library.entries[bookId]
    const busy = !!library.busy[bookId]
    const undo = library.undos[bookId]
    const requested = params.get('addBook') === bookId ? params.get('readingStatus') : null
    const intended = choices.find(s => s === requested)
    return <div className="catalog-library-actions" role="group" aria-label={title + ' kitaplık işlemleri'}>
        {library.loadError ? <button type="button" className="catalog-library-retry" onClick={library.retry}>Kitaplık durumu yüklenemedi · Tekrar dene</button> : <>
            {library.signedIn && intended && entry?.status !== intended && <p className="catalog-library-intent">Seçimin hazır: {labels[intended]}</p>}
            <div className="catalog-library-buttons">
                {choices.map(status => <button type="button" key={status}
                    aria-pressed={entry?.status === status}
                    className={intended === status ? 'intended' : ''}
                    disabled={library.loading || busy}
                    onClick={() => void library.change(bookId, status)}>
                    {entry?.status === status && <span aria-hidden="true">✓ </span>}{labels[status]}
                </button>)}
            </div>
            <div className="catalog-library-feedback">
                <span role="status">{library.loading ? 'Kitaplığın yükleniyor…' : busy ? 'Kaydediliyor…'
                    : undo ? (undo.previous ? 'Durum güncellendi.' : 'Kitaplığına eklendi.')
                    : entry ? labels[entry.status] : ''}</span>
                {undo && <button type="button" disabled={busy} onClick={() => void library.undo(bookId)}>Geri al</button>}
            </div>
            {entry?.status === 'Read' && undo && <Link className="catalog-rate-later" to={'/catalog/books/' + bookId}>İstersen değerlendir →</Link>}
        </>}
        {library.errors[bookId] && <p role="alert" className="catalog-library-error">{library.errors[bookId]}</p>}
    </div>
}
