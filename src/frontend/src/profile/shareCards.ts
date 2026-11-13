import { drawCardFrame, ink, layoutSections, profileUrl, sepia, serif, wrap, type CardFormat, type Section, type SignatureBook } from './tasteCardImage'

async function toBlob(canvas: HTMLCanvasElement) {
    return await new Promise<Blob>((resolve, reject) => canvas.toBlob(b => b ? resolve(b) : reject(new Error('empty')), 'image/png'))
}

/** A book cover with a soft shadow, or nothing when the cover could not be loaded. */
function drawCover(ctx: CanvasRenderingContext2D, cover: HTMLImageElement | null, x: number, y: number, w: number, h: number) {
    if (!cover) return
    ctx.save()
    ctx.shadowColor = 'rgba(58, 36, 20, .4)'; ctx.shadowBlur = 28; ctx.shadowOffsetY = 12
    ctx.drawImage(cover, x, y, w, h)
    ctx.restore()
}

export type CompatibilityShare = {
    viewer: string | null
    other: string
    percent: number
    label: string | null
    sharedGenres: string[]
    commonBooks: number
    /** One of the other reader's loved novels the viewer has not read yet. */
    suggestion: SignatureBook | null
}

/** Blend-style card: the two readers, the match as a dial, what they share and one book to take from the other. */
export async function renderCompatibilityCard(data: CompatibilityShare, format: CardFormat, linkViewer: boolean): Promise<Blob> {
    const canvas = document.createElement('canvas')
    const frame = drawCardFrame(canvas, format, { kicker: 'OKUR UYUMU' },
        { question: 'Seninle uyumumuz kaç?', address: data.viewer && linkViewer ? profileUrl(data.viewer) : window.location.host,
            detail: data.commonBooks > 0 ? `${data.commonBooks} ortak roman` : undefined })
    const { ctx, story, left, right, width } = frame
    const radius = story ? 170 : 128
    const centre = left + width / 2
    const sections: Section[] = []

    sections.push({
        height: radius * 2 + (story ? 210 : 170), priority: 0,
        draw: y => {
            ctx.textAlign = 'center'
            ctx.fillStyle = '#6b4c33'; ctx.font = `${story ? 44 : 38}px ${serif}`
            const names = data.viewer ? `@${data.viewer}  ·  @${data.other}` : `@${data.other} ile`
            ctx.fillText(wrap(ctx, names, width, 1)[0], centre, y + 40)
            const cy = y + 80 + radius
            ctx.lineCap = 'round'
            ctx.strokeStyle = '#eadbc4'; ctx.lineWidth = story ? 26 : 20
            ctx.beginPath(); ctx.arc(centre, cy, radius, 0, Math.PI * 2); ctx.stroke()
            ctx.strokeStyle = '#8a6143'
            ctx.beginPath(); ctx.arc(centre, cy, radius, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * Math.max(.02, data.percent / 100)); ctx.stroke()
            ctx.fillStyle = ink; ctx.font = `${story ? 150 : 116}px ${serif}`
            ctx.fillText('%' + data.percent, centre, cy + (story ? 52 : 40))
            if (data.label) {
                ctx.fillStyle = '#7a3b2e'; ctx.font = `italic ${story ? 54 : 44}px ${serif}`
                ctx.fillText(wrap(ctx, data.label, width, 1)[0], centre, cy + radius + (story ? 105 : 85))
            }
            ctx.textAlign = 'left'
        },
    })
    if (data.sharedGenres.length) sections.push({
        height: story ? 130 : 110, priority: 1,
        draw: y => {
            ctx.textAlign = 'center'
            ctx.fillStyle = sepia; ctx.font = `600 26px ${serif}`; ctx.fillText('ORTAK RAFIMIZ', centre, y + 34)
            ctx.fillStyle = ink; ctx.font = `${story ? 42 : 36}px ${serif}`
            ctx.fillText(wrap(ctx, data.sharedGenres.slice(0, 3).join(' · '), width, 1)[0], centre, y + (story ? 95 : 82))
            ctx.textAlign = 'left'
        },
    })
    const suggestion = data.suggestion
    if (suggestion) {
        const coverH = story ? 260 : 170, coverW = Math.round(coverH * .66)
        sections.push({
            height: coverH + 20, priority: 2,
            draw: y => {
                drawCover(ctx, suggestion.cover, left, y + 10, coverW, coverH)
                const textLeft = suggestion.cover ? left + coverW + 40 : left
                ctx.fillStyle = sepia; ctx.font = `600 26px ${serif}`
                ctx.fillText(wrap(ctx, `@${data.other.toLocaleUpperCase('tr-TR')} SANA ÖNERİYOR`, right - textLeft, 1)[0], textLeft, y + 60)
                ctx.fillStyle = ink; ctx.font = `${story ? 46 : 40}px ${serif}`
                const lines = wrap(ctx, suggestion.title, right - textLeft, 2)
                lines.forEach((line, i) => ctx.fillText(line, textLeft, y + 118 + i * (story ? 56 : 48)))
                if (suggestion.author) {
                    ctx.fillStyle = '#6b4c33'; ctx.font = `italic 30px ${serif}`
                    ctx.fillText(suggestion.author, textLeft, y + 128 + lines.length * (story ? 56 : 48))
                }
            },
        })
    }
    layoutSections(frame, sections)
    return toBlob(canvas)
}

export type BookShare = {
    kind: 'reading' | 'finished' | 'rating'
    nickname: string | null
    book: SignatureBook
    /** Overall score out of 10 for a rating card. */
    score?: number | null
    /** The reader's own short summary line (our original book summary). */
    summary?: string | null
}

/** "Now reading", "Finished" or "My rating" card for one book, with its cover as the hero. */
export async function renderBookCard(data: BookShare, format: CardFormat, linkProfile: boolean): Promise<Blob> {
    const canvas = document.createElement('canvas')
    const rating = data.kind === 'rating'
    const kicker = rating ? 'PUANIM' : data.kind === 'finished' ? 'OKUDUM' : 'ŞU AN OKUYORUM'
    const question = rating ? 'Sen kaç verirdin?' : data.kind === 'finished' ? 'Sen okudun mu?' : 'Sen ne okuyorsun?'
    const frame = drawCardFrame(canvas, format, { kicker, owner: data.nickname },
        { question,
            address: data.nickname && linkProfile ? profileUrl(data.nickname) : window.location.host })
    const { ctx, story, left, width } = frame
    const centre = left + width / 2
    const coverH = story ? 620 : 380, coverW = Math.round(coverH * .66)
    const sections: Section[] = []
    sections.push({
        height: (data.book.cover ? coverH + 40 : 0) + (story ? 190 : 150), priority: 0,
        draw: y => {
            if (data.book.cover) drawCover(ctx, data.book.cover, centre - coverW / 2, y + 10, coverW, coverH)
            const textTop = y + (data.book.cover ? coverH + 40 : 0)
            ctx.textAlign = 'center'
            ctx.fillStyle = ink; ctx.font = `${story ? 58 : 46}px ${serif}`
            const lines = wrap(ctx, data.book.title, width, 2)
            lines.forEach((line, i) => ctx.fillText(line, centre, textTop + (story ? 66 : 52) + i * (story ? 66 : 54)))
            if (data.book.author) {
                ctx.fillStyle = '#6b4c33'; ctx.font = `italic ${story ? 34 : 30}px ${serif}`
                ctx.fillText(data.book.author, centre, textTop + (story ? 80 : 64) + lines.length * (story ? 66 : 54))
            }
            ctx.textAlign = 'left'
        },
    })
    if (rating && data.score != null) sections.push({
        height: story ? 150 : 110, priority: 0,
        draw: y => {
            ctx.textAlign = 'center'
            ctx.fillStyle = '#7a3b2e'; ctx.font = `${story ? 120 : 88}px ${serif}`
            ctx.fillText(data.score!.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }), centre - (story ? 40 : 30), y + (story ? 115 : 85))
            ctx.fillStyle = sepia; ctx.font = `${story ? 44 : 34}px ${serif}`
            ctx.textAlign = 'left'
            ctx.fillText('/ 10', centre + (story ? 75 : 55), y + (story ? 115 : 85))
        },
    })
    if (data.summary) {
        ctx.font = `italic ${story ? 34 : 30}px ${serif}`
        const lines = wrap(ctx, data.summary, width, story ? 4 : 2)
        sections.push({
            height: lines.length * (story ? 48 : 42) + 10, priority: 3,
            draw: y => {
                ctx.fillStyle = '#5b4535'; ctx.font = `italic ${story ? 34 : 30}px ${serif}`; ctx.textAlign = 'center'
                lines.forEach((line, i) => ctx.fillText(line, centre, y + 36 + i * (story ? 48 : 42)))
                ctx.textAlign = 'left'
            },
        })
    }
    layoutSections(frame, sections)
    return toBlob(canvas)
}
