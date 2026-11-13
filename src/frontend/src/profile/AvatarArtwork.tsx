// Ex libris seals: a thin engraved emblem on a deep-toned medallion, like a bookplate stamp.
// Colours come from CSS custom properties on the face: --bg (medallion), --line (engraving), --gold (detail).
const star = (x: number, y: number, r: number) =>
    `M${x} ${y - r}l${r * .28} ${r * .72} ${r * .72} ${r * .28}-${r * .72} ${r * .28}-${r * .28} ${r * .72}-${r * .28}-${r * .72}-${r * .72}-${r * .28} ${r * .72}-${r * .28}Z`

const emblems: Record<string, React.ReactNode> = {
    owl: <>
        <path d="M28 31l2.5-8.5 6 5.5q3.5-1 7 0l6-5.5 2.5 8.5q4.5 11-.5 20.5Q46.5 59 40 59t-11.5-7.5Q23.5 42 28 31Z" />
        <circle cx="35" cy="35" r="4.4" /><circle cx="45" cy="35" r="4.4" />
        <circle cx="35" cy="35" r="1.3" className="gold-fill" /><circle cx="45" cy="35" r="1.3" className="gold-fill" />
        <path d="M38.6 40.5 40 43.5l1.4-3" className="gold" />
        <path d="M34 48.5l2 2 2-2m4 0 2 2 2-2M37 53.5l1.5 1.5 1.5-1.5 1.5 1.5 1.5-1.5" opacity=".7" />
    </>,
    quill: <>
        <path d="M55 17C44 20 33 33 27.5 59" />
        <path d="M55 17c2 11-6 25-24.5 33.5M55 17C45 17 34 26 29 44" />
        <path d="M49 25l-6 3m3.5 4.5L39 35m4 3.5-7 2" opacity=".6" />
        <path d="M27.5 59 26 63.5" />
        <path d="M21 64h20" className="gold" />
    </>,
    lantern: <>
        <circle cx="40" cy="16.5" r="2.8" />
        <path d="M33 24h14l-3-4.5h-8Z" />
        <path d="M33.5 24h13v25h-13ZM37.5 24v25m5-25v25" />
        <path d="M31 49h18v4H31Z" />
        <path d="M40 31.5q4 5 0 10.5-4-5.5 0-10.5Z" className="gold" />
    </>,
    moon: <>
        <path d="M47 19.5a21 21 0 1 0 0 41 16.5 16.5 0 1 1 0-41Z" />
        <path d={star(54, 30, 4)} className="gold" />
        <path d={star(57, 46, 2.6)} className="gold" />
        <circle cx="50.5" cy="57" r=".9" className="gold-fill" />
    </>,
    compass: <>
        <circle cx="40" cy="40" r="17" />
        <path d="M40 19v4m0 34v4M19 40h4m34 0h4" />
        <path d="M40 26 43.5 40H36.5Z" className="gold" />
        <path d="M40 54 43.5 40H36.5Z" />
        <circle cx="40" cy="40" r="1.5" className="gold-fill" />
    </>,
    key: <>
        <circle cx="29" cy="29" r="8.5" /><circle cx="29" cy="29" r="3" className="gold" />
        <path d="M35 35 58 58" />
        <path d="M50.5 50.5l-4.5 4.5m8-1-4.5 4.5" />
    </>,
    hourglass: <>
        <path d="M27 19h26M27 61h26" />
        <path d="M31 19q0 13 9 21-9 8-9 21m18-42q0 13-9 21 9 8 9 21" />
        <path d="M34.5 25h11q-2 6-5.5 9-3.5-3-5.5-9Z" className="gold" />
        <path d="M34.5 57q5.5-6.5 11 0Z" className="gold" />
        <path d="M40 40v12" opacity=".5" strokeDasharray="1 2.5" />
    </>,
    rose: <>
        <path d="M40 21q11 2 11 12.5Q51 44 40 44T29 33.5Q29 23 40 21Z" />
        <path d="M40 27.5q5.5 1 5.5 6 0 4.5-5.5 4.5-4.5 0-4.5-4.5 0-3 3-4" className="gold" />
        <path d="M40 44q-2.5 9.5 0 19" />
        <path d="M39.5 52q-8.5-3-11 3 7.5 2.5 11-3Zm.5 4q7.5-3.5 10 1.5-6.5 3-10-1.5Z" />
    </>,
    book: <>
        <path d="M17 29q11.5-4.5 23 2 11.5-6.5 23-2v27q-11.5-4.5-23 2-11.5-6.5-23-2Z" />
        <path d="M40 31v27" />
        <path d="M22 36.5q7.5-2 13.5 1M22 42.5q7.5-2 13.5 1M22 48.5q7.5-2 13.5 1M44.5 37.5q6-3 13.5-1M44.5 43.5q6-3 13.5-1" opacity=".55" />
        <path d="M51 52v10l2.5-2.5 2.5 2.5V51" className="gold" />
    </>,
    tree: <>
        <path d="M40 17q12 0 13.5 11 8.5 3.5 5.5 12.5-3 6.5-10.5 5H31.5Q24 47 21 40.5 18 31.5 26.5 28 28 17 40 17Z" />
        <path d="M40 62V41m0 7-6-6m6 8.5 7-7" />
        <path d="M33 62q7-3.5 14 0" className="gold" />
    </>,
    anchor: <>
        <circle cx="40" cy="19.5" r="4" className="gold" />
        <path d="M40 23.5V61M32 30h16" />
        <path d="M23.5 47q2 13 16.5 14 14.5-1 16.5-14" />
        <path d="M23.5 47 20 51.5m3.5-4.5 4.5 2M56.5 47l3.5 4.5M56.5 47 52 49" />
    </>,
    sun: <>
        <circle cx="40" cy="40" r="9.5" className="gold" />
        <path d="M40 18v7m0 30v7M18 40h7m30 0h7M24.4 24.4l5 5m21.2 21.2 5 5M24.4 55.6l5-5m21.2-21.2 5-5" />
        <path d="M33.5 18.8l1.4 4.6m10.2 33.2 1.4 4.6M18.8 46.5l4.6-1.4m33.2-10.2 4.6-1.4" opacity=".5" />
    </>,
}

export default function AvatarArtwork({ kind }: { kind: string }) {
    return <svg viewBox="0 0 80 80" aria-hidden="true" focusable="false" className="ex-libris">
        <circle cx="40" cy="40" r="35.5" className="seal-ring" />
        <circle cx="40" cy="40" r="32.3" className="seal-dots" />
        <g className="seal-emblem" transform="translate(40 40) scale(1.16) translate(-40 -40)">{emblems[kind] ?? emblems.owl}</g>
    </svg>
}
