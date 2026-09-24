import { createContext } from 'react'
import type { AchievementTrack } from './AchievementTracks'

export const ReaderBadgesContext = createContext<AchievementTrack[]>([])
export const avatars = [
    { id: 'owl', label: 'Baykuş', emoji: '🦉' }, { id: 'fox', label: 'Tilki', emoji: '🦊' },
    { id: 'cat', label: 'Kedi', emoji: '🐱' }, { id: 'bear', label: 'Ayı', emoji: '🐻' },
    { id: 'deer', label: 'Geyik', emoji: '🦌' }, { id: 'panda', label: 'Panda', emoji: '🐼' },
    { id: 'penguin', label: 'Penguen', emoji: '🐧' }, { id: 'rabbit', label: 'Tavşan', emoji: '🐰' },
]
