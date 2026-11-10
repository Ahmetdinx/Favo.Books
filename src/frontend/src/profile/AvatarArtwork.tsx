// Flat animal portraits. Colours come from CSS custom properties on the face:
// --fur (main), --fur2 (light patches), --ink (eyes and dark marks), --accent (beak, nose, inner ear).
// Existing avatar IDs remain stable for saved profiles.
const eye = (cx: number, cy: number, rx = 2.6, ry = 3.1) => <>
    <ellipse cx={cx} cy={cy} rx={rx} ry={ry} fill="var(--ink)" />
    <circle cx={cx + rx * .35} cy={cy - ry * .4} r={rx * .38} fill="#fff" />
</>
const cheeks = (y: number, spread = 13) => <>
    <ellipse cx={40 - spread} cy={y} rx="3.6" ry="2.2" fill="var(--blush, #e88b6f)" opacity=".35" />
    <ellipse cx={40 + spread} cy={y} rx="3.6" ry="2.2" fill="var(--blush, #e88b6f)" opacity=".35" />
</>

const faces: Record<string, React.ReactNode> = {
    owl: <>
        <path d="M18 34 21 13l11 10q8-3 16 0l11-10 3 21q4 26-22 34-26-8-22-34Z" fill="var(--fur)" />
        <path d="M24 60q16 10 32 0-6 9-16 10-10-1-16-10Z" fill="var(--fur2)" opacity=".5" />
        <circle cx="30.5" cy="37" r="10" fill="var(--fur2)" />
        <circle cx="49.5" cy="37" r="10" fill="var(--fur2)" />
        {eye(30.5, 37, 4.2, 4.2)}
        {eye(49.5, 37, 4.2, 4.2)}
        <path d="M36.5 45h7L40 51Z" fill="var(--accent)" />
        <path d="M34 58q2 2 4 0m4 0q2 2 4 0" stroke="var(--fur2)" strokeWidth="1.6" strokeLinecap="round" fill="none" />
    </>,
    fox: <>
        <path d="M15 13 33 25 22 38Z" fill="var(--fur)" />
        <path d="M65 13 47 25 58 38Z" fill="var(--fur)" />
        <path d="M19 19 28 26l-5 6Z" fill="var(--ink)" opacity=".35" />
        <path d="M61 19 52 26l5 6Z" fill="var(--ink)" opacity=".35" />
        <path d="M13 36q5-14 27-14t27 14q-4 16-27 30Q17 52 13 36Z" fill="var(--fur)" />
        <path d="M17 40q12 0 23 13 11-13 23-13-5 13-23 26-18-13-23-26Z" fill="var(--fur2)" />
        {eye(30, 38)}
        {eye(50, 38)}
        <ellipse cx="40" cy="58" rx="3.4" ry="2.5" fill="var(--ink)" />
    </>,
    cat: <>
        <path d="M17 36 19 12l17 13Z" fill="var(--fur)" />
        <path d="M63 36 61 12 44 25Z" fill="var(--fur)" />
        <path d="M21.5 27 22.5 18l8 6Z" fill="var(--accent)" opacity=".75" />
        <path d="M58.5 27 57.5 18l-8 6Z" fill="var(--accent)" opacity=".75" />
        <ellipse cx="40" cy="43" rx="25" ry="21" fill="var(--fur)" />
        <path d="M33 23q7 4 14 0" stroke="var(--ink)" strokeWidth="1.6" strokeLinecap="round" opacity=".25" fill="none" />
        <ellipse cx="40" cy="52" rx="10.5" ry="7.5" fill="var(--fur2)" />
        {eye(30.5, 41, 2.8, 3.6)}
        {eye(49.5, 41, 2.8, 3.6)}
        <path d="M37.4 48.5h5.2L40 51.6Z" fill="var(--accent)" />
        <path d="M17 49l10 1m-9 5 9-2m36-4-10 1m9 5-9-2" stroke="var(--ink)" strokeWidth="1" strokeLinecap="round" opacity=".35" />
        {cheeks(50, 15)}
    </>,
    bear: <>
        <circle cx="21" cy="24" r="9" fill="var(--fur)" />
        <circle cx="59" cy="24" r="9" fill="var(--fur)" />
        <circle cx="21" cy="24" r="4.5" fill="var(--fur2)" />
        <circle cx="59" cy="24" r="4.5" fill="var(--fur2)" />
        <ellipse cx="40" cy="42" rx="25" ry="23" fill="var(--fur)" />
        <ellipse cx="40" cy="51" rx="11.5" ry="9" fill="var(--fur2)" />
        {eye(30, 38)}
        {eye(50, 38)}
        <ellipse cx="40" cy="47.5" rx="4.2" ry="3" fill="var(--ink)" />
        <path d="M40 50.5v3.5m-3.5 0q3.5 2.5 7 0" stroke="var(--ink)" strokeWidth="1.3" strokeLinecap="round" fill="none" />
        {cheeks(46, 15)}
    </>,
    deer: <>
        <path d="M27 21q-6-6-5-14m5 14q-10-3-13-9m40 9q6-6 5-14m-5 14q10-3 13-9" stroke="var(--accent)" strokeWidth="2.6" strokeLinecap="round" fill="none" />
        <path d="M27 31q-14 2-18-8 12-3 20 3Z" fill="var(--fur)" />
        <path d="M53 31q14 2 18-8-12-3-20 3Z" fill="var(--fur)" />
        <path d="M24 29q-7 0-10-5 6-1 11 2Z" fill="var(--fur2)" />
        <path d="M56 29q7 0 10-5-6-1-11 2Z" fill="var(--fur2)" />
        <path d="M24 34q0-14 16-14t16 14q0 10-6 20-4 12-10 12t-10-12q-6-10-6-20Z" fill="var(--fur)" />
        <path d="M32 54q8-5 16 0-2 12-8 12t-8-12Z" fill="var(--fur2)" />
        <circle cx="34" cy="27" r="1.4" fill="var(--fur2)" />
        <circle cx="46" cy="27" r="1.4" fill="var(--fur2)" />
        <circle cx="40" cy="25" r="1.2" fill="var(--fur2)" />
        {eye(32, 39)}
        {eye(48, 39)}
        <ellipse cx="40" cy="58" rx="3.6" ry="2.6" fill="var(--ink)" />
    </>,
    panda: <>
        <circle cx="21" cy="23" r="8.5" fill="var(--ink)" />
        <circle cx="59" cy="23" r="8.5" fill="var(--ink)" />
        <ellipse cx="40" cy="42" rx="26" ry="23" fill="var(--fur2)" />
        <ellipse cx="29.5" cy="40" rx="6" ry="8" transform="rotate(28 29.5 40)" fill="var(--ink)" />
        <ellipse cx="50.5" cy="40" rx="6" ry="8" transform="rotate(-28 50.5 40)" fill="var(--ink)" />
        <circle cx="30.5" cy="39" r="2.3" fill="#fff" />
        <circle cx="49.5" cy="39" r="2.3" fill="#fff" />
        <circle cx="31" cy="39.4" r="1.2" fill="var(--ink)" />
        <circle cx="49" cy="39.4" r="1.2" fill="var(--ink)" />
        <ellipse cx="40" cy="50" rx="4" ry="2.8" fill="var(--ink)" />
        <path d="M40 52.6v2.8m-3.2 0q3.2 2.2 6.4 0" stroke="var(--ink)" strokeWidth="1.2" strokeLinecap="round" fill="none" />
        {cheeks(50, 16)}
    </>,
    penguin: <>
        <path d="M14 46q0-30 26-30t26 30q0 22-26 22T14 46Z" fill="var(--ink)" />
        <path d="M40 31q-6-5-12-3-9 4-7 17 2 15 19 18 17-3 19-18 2-13-7-17-6-2-12 3Z" fill="var(--fur2)" />
        {eye(31, 41, 2.5, 3)}
        {eye(49, 41, 2.5, 3)}
        <path d="M35.5 46.5q4.5-2.5 9 0L40 52Z" fill="var(--accent)" />
        {cheeks(49, 13)}
    </>,
    rabbit: <>
        <path d="M24 32q-8-18-4-26 7-3 12 8 3 8 3 18Z" fill="var(--fur)" />
        <path d="M56 32q8-18 4-26-7-3-12 8-3 8-3 18Z" fill="var(--fur)" />
        <path d="M26 28q-5-12-3-18 4-1 7 6 2 6 2 12Z" fill="var(--accent)" opacity=".7" />
        <path d="M54 28q5-12 3-18-4-1-7 6-2 6-2 12Z" fill="var(--accent)" opacity=".7" />
        <ellipse cx="40" cy="46" rx="22" ry="20" fill="var(--fur)" />
        <ellipse cx="40" cy="54" rx="9" ry="6.5" fill="var(--fur2)" />
        {eye(31, 43)}
        {eye(49, 43)}
        <path d="M37.5 50.5h5L40 53.2Z" fill="var(--accent)" />
        <path d="M38.2 56h3.6v3.2h-3.6Z" fill="#fff" stroke="var(--ink)" strokeWidth=".6" opacity=".9" />
        {cheeks(52, 14)}
    </>,
}

export default function AvatarArtwork({ kind }: { kind: string }) {
    return <svg viewBox="0 0 80 80" aria-hidden="true" focusable="false">
        <circle cx="40" cy="86" r="34" fill="var(--fur)" opacity=".18" />
        {faces[kind] ?? faces.owl}
    </svg>
}
