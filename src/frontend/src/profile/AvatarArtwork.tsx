// Original bookplate illustrations. Existing avatar IDs remain stable for saved profiles.
export default function AvatarArtwork({ kind }: { kind: string }) {
    const faces: Record<string, React.ReactNode> = {
        owl: <><path d="M23 28 20 16 31 23Q40 19 49 23L60 16 57 29V39Q40 55 23 39Z" /><circle cx="31" cy="32" r="7" /><circle cx="49" cy="32" r="7" /><circle cx="31" cy="32" r="1.6" fill="currentColor" /><circle cx="49" cy="32" r="1.6" fill="currentColor" /><path d="m37 40 3 4 3-4" /></>,
        fox: <><path d="m20 18 17 10h6l17-10-4 24-16 10-16-10Z" /><path d="m24 31 16 15 16-15M28 34l5 2m14 0 5-2m-15 9h6l-3 4Z" /></>,
        cat: <><path d="M23 38V19l12 8h10l12-8v19Q55 51 40 51T23 38Z" /><path d="m29 35 4 2m14 0 4-2m-14 5h6l-3 3Zm-9 1-13-3m13 7-13 1m31-5 13-3m-13 7 13 1" /></>,
        bear: <><circle cx="24" cy="25" r="8" /><circle cx="56" cy="25" r="8" /><path d="M22 35Q22 23 40 23T58 35v5Q55 53 40 53T22 40Z" /><ellipse cx="40" cy="42" rx="9" ry="7" /><path d="M30 34h1m18 0h1m-13 6h6l-3 4Zm3 4v3" /></>,
        deer: <><path d="m30 25-8-8V10m0 7-7-3m35 11 8-8V10m0 7 7-3M29 28l-12-4 5 11 9 2m18-9 12-4-5 11-9 2M31 26h18l-3 22-6 6-6-6Z" /><path d="m34 35 2 1m8 0 2-1m-9 11h6l-3 4Z" /></>,
        panda: <><circle cx="25" cy="24" r="8" /><circle cx="55" cy="24" r="8" /><ellipse cx="40" cy="37" rx="21" ry="17" /><ellipse cx="30" cy="35" rx="5" ry="7" transform="rotate(25 30 35)" /><ellipse cx="50" cy="35" rx="5" ry="7" transform="rotate(-25 50 35)" /><path d="m37 44 3 4 3-4ZM30 35h1m18 0h1" /></>,
        penguin: <><path d="M23 48V35Q23 17 40 17T57 35v13M30 47V34q0-12 10-5 10-7 10 5v13" /><path d="M33 34h1m12 0h1m-11 4 4 5 4-5Z" /></>,
        rabbit: <><path d="M29 31Q15 7 25 8q8 0 11 22m8 0Q47 8 55 8q10-1-4 23M24 39q0-12 16-12t16 12q0 14-16 14T24 39Z" /><path d="M31 37h1m16 0h1m-12 6h6l-3 4Zm3 4v3" /></>,
    }
    return <svg viewBox="0 0 80 80" aria-hidden="true" focusable="false" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="40" cy="40" r="36" strokeWidth=".6" opacity=".4" />
        {faces[kind] ?? faces.owl}
        <path d="M14 53q13-4 26 3 13-7 26-3v14q-13-4-26 3-13-7-26-3Z" fill="var(--avatar-paper, #eddbc0)" />
        <path d="M40 56v14M20 58q8-1 15 3m-15 2q8-1 15 3m10-5q7-4 15-3m-15 8q7-4 15-3" strokeWidth="1.1" />
    </svg>
}
