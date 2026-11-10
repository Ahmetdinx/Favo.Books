export type TasteProfile = {
    ready: boolean
    readCount: number
    ratingCount: number
    booksNeeded: number
    ratingsNeeded: number
    title: string | null
    genres: { slug: string; name: string; percent: number }[]
    strengths: { key: string; label: string }[]
    traits: string[]
    averageScore: number | null
}

const W = 1080
const H = 1350

function wrap(ctx: CanvasRenderingContext2D, text: string, maxWidth: number) {
    const lines: string[] = []
    let line = ''
    for (const word of text.split(' ')) {
        const next = line ? line + ' ' + word : word
        if (ctx.measureText(next).width > maxWidth && line) { lines.push(line); line = word } else line = next
    }
    if (line) lines.push(line)
    return lines
}

/** Draws the reader card as a portrait image suited to phone sharing. */
export function drawTasteCard(canvas: HTMLCanvasElement, profile: TasteProfile, nickname: string | null) {
    canvas.width = W
    canvas.height = H
    const ctx = canvas.getContext('2d')!
    const serif = 'Georgia, "Times New Roman", serif'

    // Warm desk background and the paper card.
    const desk = ctx.createLinearGradient(0, 0, W, H)
    desk.addColorStop(0, '#d9c3a1'); desk.addColorStop(1, '#a98764')
    ctx.fillStyle = desk; ctx.fillRect(0, 0, W, H)
    ctx.save()
    ctx.shadowColor = 'rgba(58, 36, 20, .35)'; ctx.shadowBlur = 50; ctx.shadowOffsetY = 18
    ctx.fillStyle = '#fbf5ea'
    ctx.beginPath(); ctx.roundRect(90, 110, W - 180, H - 220, 14); ctx.fill()
    ctx.restore()

    // Ruled lines and the red margin of a library card.
    ctx.strokeStyle = 'rgba(122, 160, 190, .28)'; ctx.lineWidth = 2
    for (let y = 330; y < H - 150; y += 56) { ctx.beginPath(); ctx.moveTo(110, y); ctx.lineTo(W - 110, y); ctx.stroke() }
    ctx.strokeStyle = 'rgba(190, 80, 70, .45)'
    ctx.beginPath(); ctx.moveTo(190, 120); ctx.lineTo(190, H - 120); ctx.stroke()

    ctx.fillStyle = '#8a6a4a'
    ctx.font = `600 30px ${serif}`
    ctx.fillText('OKUR KİMLİĞİ', 230, 200)
    ctx.textAlign = 'right'
    ctx.font = `italic 30px ${serif}`
    ctx.fillText('№ ' + String(profile.readCount + profile.ratingCount).padStart(3, '0'), W - 140, 200)
    ctx.textAlign = 'left'
    if (nickname) { ctx.font = `34px ${serif}`; ctx.fillStyle = '#6b4c33'; ctx.fillText('@' + nickname, 230, 255) }

    ctx.fillStyle = '#3e2d24'
    ctx.font = `86px ${serif}`
    // Centre the content block between the header and the footer.
    const titleLines = wrap(ctx, profile.title ?? '', W - 360)
    const block = titleLines.length * 96 + 40 + profile.genres.length * 112 + (profile.strengths.length ? 150 : 0) + (profile.traits.length ? 70 : 0)
    let y = Math.max(400, 330 + ((H - 260) - 330 - block) / 2 + 70)
    for (const line of titleLines) { ctx.fillText(line, 230, y); y += 96 }

    y += 40
    for (const genre of profile.genres) {
        ctx.fillStyle = '#4a372a'; ctx.font = `38px ${serif}`
        ctx.fillText(genre.name, 230, y)
        ctx.textAlign = 'right'; ctx.font = `600 36px ${serif}`
        ctx.fillText('%' + genre.percent, W - 140, y)
        ctx.textAlign = 'left'
        ctx.fillStyle = '#eadbc4'; ctx.beginPath(); ctx.roundRect(230, y + 18, W - 370, 14, 7); ctx.fill()
        ctx.fillStyle = '#8a6143'; ctx.beginPath(); ctx.roundRect(230, y + 18, Math.max(14, (W - 370) * genre.percent / 100), 14, 7); ctx.fill()
        y += 112
    }

    if (profile.strengths.length) {
        y += 10
        ctx.fillStyle = '#6b4c33'; ctx.font = `italic 34px ${serif}`
        ctx.fillText('Sevdiği romanlarda öne çıkan', 230, y)
        ctx.fillStyle = '#3e2d24'; ctx.font = `600 44px ${serif}`
        ctx.fillText(profile.strengths.map(s => s.label).join(' ve '), 230, y + 60)
        y += 140
    }

    // Traits as ink stamps.
    let x = 230
    ctx.font = `600 28px ${serif}`
    for (const [i, trait] of profile.traits.entries()) {
        const width = ctx.measureText(trait.toLocaleUpperCase('tr-TR')).width + 44
        if (x + width > W - 130) break
        ctx.save()
        ctx.translate(x + width / 2, y + 10); ctx.rotate((i % 2 ? 2.5 : -2.5) * Math.PI / 180)
        ctx.strokeStyle = 'rgba(150, 60, 50, .75)'; ctx.lineWidth = 4
        ctx.beginPath(); ctx.roundRect(-width / 2, -34, width, 68, 10); ctx.stroke()
        ctx.fillStyle = 'rgba(150, 60, 50, .85)'; ctx.textAlign = 'center'
        ctx.fillText(trait.toLocaleUpperCase('tr-TR'), 0, 10)
        ctx.restore()
        x += width + 26
    }

    ctx.fillStyle = '#8a6a4a'; ctx.font = `30px ${serif}`
    ctx.fillText(`${profile.readCount} okunmuş · ${profile.ratingCount} değerlendirme`, 230, H - 170)
    ctx.textAlign = 'right'; ctx.font = `600 34px ${serif}`; ctx.fillStyle = '#5b3a26'
    ctx.fillText('favo kitap', W - 140, H - 170)
    ctx.textAlign = 'left'
}

/** Shares the card through the system share sheet when possible, otherwise downloads it. */
export async function downloadTasteCard(profile: TasteProfile, nickname: string | null): Promise<'shared' | 'downloaded'> {
    const canvas = document.createElement('canvas')
    drawTasteCard(canvas, profile, nickname)
    const blob = await new Promise<Blob>((resolve, reject) => canvas.toBlob(b => b ? resolve(b) : reject(new Error('empty')), 'image/png'))
    const file = new File([blob], 'favo-okur-kimligi.png', { type: 'image/png' })
    if (navigator.canShare?.({ files: [file] })) {
        try {
            await navigator.share({ files: [file], title: 'Okur kimliğim' })
            return 'shared'
        } catch (error) {
            if (error instanceof DOMException && error.name === 'AbortError') return 'shared'
        }
    }
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = file.name
    link.click()
    setTimeout(() => URL.revokeObjectURL(link.href), 1000)
    return 'downloaded'
}
