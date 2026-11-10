import './MarginShelves.css'

// The page margins become the two walls of a library corridor, drawn in one-point perspective
// toward the content in the middle. Purely decorative.
const W = 300
const H = 1000
const FAR_TOP = 210
const FAR_BOTTOM = 790
const SHELVES = 7
const spineColors = ['#d2bb9b', '#c8ae8c', '#bfa585', '#d8c4a5', '#c4a98d', '#cbb497', '#b9a083', '#d0b796', '#c6b09a', '#bca88e']

function random(seed: number) {
    let state = seed
    return () => {
        state = (state * 9301 + 49297) % 233280
        return state / 233280
    }
}

// Depth t in [0, 1] (near wall edge to far end) mapped to a screen x with perspective foreshortening.
const depthToX = (t: number) => W * (t * 3) / (1 + t * 2)
// Height of the wall at screen x: the top and bottom edges converge toward the vanishing point.
const topAt = (x: number) => (x / W) * FAR_TOP
const bottomAt = (x: number) => H - (x / W) * (H - FAR_BOTTOM)
const shelfY = (x: number, i: number) => topAt(x) + (bottomAt(x) - topAt(x)) * (i / SHELVES)

function Wall({ seed }: { seed: number }) {
    const next = random(seed)
    const books: React.ReactNode[] = []
    for (let shelf = 0; shelf < SHELVES; shelf++) {
        let t = .004
        while (t < .985) {
            const step = .012 + next() * .016
            const gap = next() < .05
            const t2 = Math.min(.99, t + step)
            if (!gap) {
                const x1 = depthToX(t), x2 = depthToX(t2)
                const fill = .62 + next() * .3
                const base1 = shelfY(x1, shelf + 1) - 3 * (1 - t), base2 = shelfY(x2, shelf + 1) - 3 * (1 - t2)
                const top1 = base1 - (base1 - shelfY(x1, shelf)) * fill, top2 = base2 - (base2 - shelfY(x2, shelf)) * fill
                const color = spineColors[Math.floor(next() * spineColors.length)]
                const band = next()
                books.push(<g key={`${shelf}-${t}`}>
                    <polygon points={`${x1},${top1} ${x2},${top2} ${x2},${base2} ${x1},${base1}`} fill={color} />
                    <polygon points={`${x1},${top1} ${x2},${top2} ${x2},${base2} ${x1},${base1}`} fill="none" stroke="#9c7f60" strokeOpacity=".35" strokeWidth=".6" />
                    {band < .55 && <line x1={x1} y1={top1 + (base1 - top1) * .14} x2={x2} y2={top2 + (base2 - top2) * .14}
                        stroke="#a8875e" strokeOpacity=".35" strokeWidth={Math.max(.5, 1.4 * (1 - t))} />}
                    {band < .25 && <line x1={x1} y1={top1 + (base1 - top1) * .2} x2={x2} y2={top2 + (base2 - top2) * .2}
                        stroke="#a8875e" strokeOpacity=".25" strokeWidth={Math.max(.4, 1 * (1 - t))} />}
                </g>)
            }
            t = t2 + .0015
        }
    }

    const shelves = Array.from({ length: SHELVES + 1 }, (_, i) => {
        const yNear = shelfY(0, i), yFar = shelfY(W, i)
        const thickNear = 5, thickFar = 1.6
        return <polygon key={i} fill="url(#mr-board)"
            points={`0,${yNear - thickNear / 2} ${W},${yFar - thickFar / 2} ${W},${yFar + thickFar / 2} 0,${yNear + thickNear / 2}`} />
    })
    const uprights = [.18, .42, .66, .88].map(t => {
        const x = depthToX(t), w = 5 * (1 - t) + 1.2
        return <polygon key={t} fill="url(#mr-post)"
            points={`${x - w / 2},${topAt(x - w / 2)} ${x + w / 2},${topAt(x + w / 2)} ${x + w / 2},${bottomAt(x + w / 2)} ${x - w / 2},${bottomAt(x - w / 2)}`} />
    })

    return <>
        <defs>
            <linearGradient id="mr-board" x1="0" x2="1">
                <stop offset="0" stopColor="#a8896a" />
                <stop offset="1" stopColor="#c4a888" />
            </linearGradient>
            <linearGradient id="mr-post" x1="0" x2="1">
                <stop offset="0" stopColor="#ad9070" />
                <stop offset="1" stopColor="#c7ad8f" />
            </linearGradient>
            <linearGradient id="mr-fade" x1="0" x2="1">
                <stop offset="0" stopColor="#fff" stopOpacity=".55" />
                <stop offset=".35" stopColor="#fff" stopOpacity=".7" />
                <stop offset="1" stopColor="#fff" stopOpacity="0" />
            </linearGradient>
            <mask id="mr-mask"><rect width={W} height={H} fill="url(#mr-fade)" /></mask>
        </defs>
        <g mask="url(#mr-mask)">
            {Array.from({ length: 9 }, (_, i) => <line key={i} x1={W} y1={FAR_BOTTOM} x2={W - (i + 1) * 70} y2={H + 40} stroke="#8b6b4c" strokeOpacity=".14" />)}
            {Array.from({ length: 5 }, (_, i) => <line key={i} x1={W} y1={FAR_TOP} x2={W - (i + 1) * 90} y2={-40} stroke="#8b6b4c" strokeOpacity=".1" />)}
            {books}
            {shelves}
            {uprights}
            <line x1="0" y1="0" x2={W} y2={FAR_TOP} stroke="#8b6b4c" strokeOpacity=".35" strokeWidth="1.2" />
            <line x1="0" y1={H} x2={W} y2={FAR_BOTTOM} stroke="#8b6b4c" strokeOpacity=".35" strokeWidth="1.2" />
        </g>
    </>
}

export default function MarginShelves() {
    return <div className="margin-shelves" aria-hidden="true">
        <svg className="margin-wall margin-wall-left" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none"><Wall seed={17} /></svg>
        <svg className="margin-wall margin-wall-right" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none"><Wall seed={61} /></svg>
    </div>
}
