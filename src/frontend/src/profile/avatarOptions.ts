import { createContext } from 'react'
import type { AchievementTrack } from './AchievementTracks'

export const ReaderBadgesContext = createContext<AchievementTrack[]>([])

/** Ex libris seals. IDs are stored on the account; the server validates the same list (User.IsValidAvatar). */
export const avatars = [
    { id: 'owl', label: 'Baykuş' }, { id: 'quill', label: 'Tüy kalem' },
    { id: 'lantern', label: 'Fener' }, { id: 'moon', label: 'Hilal' },
    { id: 'compass', label: 'Pusula' }, { id: 'key', label: 'Anahtar' },
    { id: 'hourglass', label: 'Kum saati' }, { id: 'rose', label: 'Gül' },
    { id: 'book', label: 'Açık kitap' }, { id: 'tree', label: 'Hayat ağacı' },
    { id: 'anchor', label: 'Çapa' }, { id: 'sun', label: 'Güneş' },
]
