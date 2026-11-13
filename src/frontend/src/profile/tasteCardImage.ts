import { getBookSummaries } from '../catalog/catalogApi'
import { cleanBookTitle } from '../catalog/bookTitle'

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
    signatureBookId?: string | null
}

/** Instagram/WhatsApp story (9:16) or feed post (4:5). */
export type CardFormat = 'story' | 'post'

export type SignatureBook = { title: string; author: string | null; cover: HTMLImageElement | null }

const W = 1080
const HEIGHT: Record<CardFormat, number> = { story: 1920, post: 1350 }
const serif = 'Georgia, "Times New Roman", serif'
const ink = '#3e2d24'
const sepia = '#8a6a4a'

/** The public profile address printed on the card, so viewers can find the reader (and Favo). */
export function profileUrl(nickname: string) {
    return new URL(import.meta.env.BASE_URL + 'okur/' + encodeURIComponent(nickname), window.location.origin).href
}

function wrap(ctx: CanvasRenderingContext2D, text: string, maxWidth: number, maxLines = 3) {
    const lines: string[] = []
    let line = ''
    for (const word of text.split(' ')) {
        const next = line ? line + ' ' + word : word
        if (ctx.measureText(next).width > maxWidth && line) { lines.push(line); line = word } else line = next
    }
    if (line) lines.push(line)
    if (lines.length > maxLines) {
        lines.length = maxLines
        lines[maxLines - 1] = lines[maxLines - 1].replace(/\s*\S*$/, '') + '…'
    }
    return lines
}

/** Loads the signature book's title and same-origin cover; the card is still drawn if either fails. */
export async function loadSignatureBook(bookId: string | null | undefined): Promise<SignatureBook | null> {
    if (!bookId) return null
    try {
        const [summary] = (await getBookSummaries({ bookIds: [bookId] })).items
        if (!summary) return null
        const cover = await new Promise<HTMLImageElement | null>(resolve => {
            const image = new Image()
            image.onload = () => resolve(image)
            image.onerror = () => resolve(null)
            image.src = `/api/catalog/books/${encodeURIComponent(bookId)}/cover`
        })
        return { title: cleanBookTitle(summary.title), author: summary.authors[0]?.name ?? null, cover }
    } catch { return null }
}

/** priority: lower is more essential; the title (0) is always drawn. */
type Section = { height: number; priority: number; draw: (y: number) => void }

/** Draws the reader identity card. Sections are measured first and the block is centred on the card. */
export function drawTasteCard(canvas: HTMLCanvasElement, profile: TasteProfile, nickname: string | null,
    format: CardFormat = 'post', signature: SignatureBook | null = null, linkProfile = true) {
    const H = HEIGHT[format]
    const story = format === 'story'
    canvas.width = W
    canvas.height = H
    const ctx = canvas.getContext('2d')!
    const left = 210, right = W - 130, width = right - left
    const top = 110, bottom = H - 110

    // Warm desk and the paper library card.
    const desk = ctx.createLinearGradient(0, 0, W, H)
    desk.addColorStop(0, '#d9c3a1'); desk.addColorStop(1, '#a3815f')
    ctx.fillStyle = desk; ctx.fillRect(0, 0, W, H)
    ctx.save()
    ctx.shadowColor = 'rgba(58, 36, 20, .35)'; ctx.shadowBlur = 50; ctx.shadowOffsetY = 18
    ctx.fillStyle = '#fbf5ea'
    ctx.beginPath(); ctx.roundRect(80, top, W - 160, bottom - top, 16); ctx.fill()
    ctx.restore()
    ctx.strokeStyle = 'rgba(122, 160, 190, .22)'; ctx.lineWidth = 2
    for (let y = top + 220; y < bottom - 40; y += 56) { ctx.beginPath(); ctx.moveTo(100, y); ctx.lineTo(W - 100, y); ctx.stroke() }
    ctx.strokeStyle = 'rgba(190, 80, 70, .45)'
    ctx.beginPath(); ctx.moveTo(170, top + 10); ctx.lineTo(170, bottom - 10); ctx.stroke()

    // Header.
    ctx.fillStyle = sepia; ctx.font = `600 30px ${serif}`
    ctx.fillText('OKUR KİMLİĞİ', left, top + 90)
    ctx.textAlign = 'right'; ctx.font = `italic 30px ${serif}`
    ctx.fillText('№ ' + String(profile.readCount + profile.ratingCount).padStart(3, '0'), right, top + 90)
    ctx.textAlign = 'left'
    if (nickname) { ctx.font = `36px ${serif}`; ctx.fillStyle = '#6b4c33'; ctx.fillText('@' + nickname, left, top + 145) }

    // Footer: the way back to Favo.
    const footerTop = bottom - (story ? 230 : 170)
    ctx.strokeStyle = 'rgba(138, 106, 74, .35)'; ctx.lineWidth = 2
    ctx.beginPath(); ctx.moveTo(left, footerTop); ctx.lineTo(right, footerTop); ctx.stroke()
    ctx.fillStyle = ink; ctx.font = `italic ${story ? 44 : 38}px ${serif}`
    ctx.fillText('Sen hangi okursun?', left, footerTop + (story ? 75 : 62))
    ctx.fillStyle = sepia; ctx.font = `${story ? 30 : 27}px ${serif}`
    const address = nickname && linkProfile ? profileUrl(nickname).replace(/^https?:\/\//, '') : window.location.host
    ctx.fillText(wrap(ctx, address, width - 200, 1)[0], left, footerTop + (story ? 130 : 110))
    ctx.textAlign = 'right'; ctx.font = `600 ${story ? 46 : 40}px ${serif}`; ctx.fillStyle = '#5b3a26'
    ctx.fillText('favo', right, footerTop + (story ? 75 : 62))
    ctx.font = `${story ? 26 : 24}px ${serif}`; ctx.fillStyle = sepia
    ctx.fillText(`${profile.readCount} okunmuş · ${profile.ratingCount} puan`, right, footerTop + (story ? 130 : 110))
    ctx.textAlign = 'left'

    // Body sections, measured before drawing.
    const titleSize = story ? 100 : 78
    ctx.font = `${titleSize}px ${serif}`
    const titleLines = wrap(ctx, profile.title ?? '', width)
    const sections: Section[] = []
    sections.push({
        height: titleLines.length * titleSize * 1.1 + 20, priority: 0,
        draw: y => {
            ctx.fillStyle = ink; ctx.font = `${titleSize}px ${serif}`
            titleLines.forEach((line, i) => ctx.fillText(line, left, y + titleSize * (i + 0.85) * 1.1))
        },
    })
    const genreRow = story ? 118 : 86
    if (profile.genres.length) sections.push({
        height: profile.genres.length * genreRow, priority: 1,
        draw: y => profile.genres.forEach((genre, i) => {
            const row = y + i * genreRow + 44
            ctx.fillStyle = '#4a372a'; ctx.font = `38px ${serif}`; ctx.fillText(genre.name, left, row)
            ctx.textAlign = 'right'; ctx.font = `600 36px ${serif}`; ctx.fillText('%' + genre.percent, right, row)
            ctx.textAlign = 'left'
            ctx.fillStyle = '#eadbc4'; ctx.beginPath(); ctx.roundRect(left, row + 18, width, 14, 7); ctx.fill()
            ctx.fillStyle = '#8a6143'; ctx.beginPath(); ctx.roundRect(left, row + 18, Math.max(14, width * genre.percent / 100), 14, 7); ctx.fill()
        }),
    })
    if (profile.strengths.length) sections.push({
        height: 130, priority: 3,
        draw: y => {
            ctx.fillStyle = '#6b4c33'; ctx.font = `italic 32px ${serif}`; ctx.fillText('Sevdiği romanlarda öne çıkan', left, y + 40)
            ctx.fillStyle = ink; ctx.font = `600 42px ${serif}`
            ctx.fillText(wrap(ctx, profile.strengths.map(s => s.label).join(' ve '), width, 1)[0], left, y + 98)
        },
    })
    if (profile.traits.length) sections.push({
        height: 100, priority: 4,
        draw: y => {
            let x = left
            ctx.font = `600 27px ${serif}`
            for (const [i, trait] of profile.traits.entries()) {
                const label = trait.toLocaleUpperCase('tr-TR')
                const stamp = ctx.measureText(label).width + 44
                if (x + stamp > right) break
                ctx.save()
                ctx.translate(x + stamp / 2, y + 50); ctx.rotate((i % 2 ? 2.5 : -2.5) * Math.PI / 180)
                ctx.strokeStyle = 'rgba(150, 60, 50, .75)'; ctx.lineWidth = 4
                ctx.beginPath(); ctx.roundRect(-stamp / 2, -32, stamp, 64, 10); ctx.stroke()
                ctx.fillStyle = 'rgba(150, 60, 50, .85)'; ctx.textAlign = 'center'; ctx.fillText(label, 0, 10)
                ctx.restore()
                x += stamp + 24
            }
        },
    })
    if (signature) {
        const coverH = story ? 300 : 180, coverW = Math.round(coverH * 0.66)
        sections.push({
            height: coverH + 30, priority: 2,
            draw: y => {
                const textLeft = signature.cover ? left + coverW + 40 : left
                if (signature.cover) {
                    ctx.save()
                    ctx.shadowColor = 'rgba(58, 36, 20, .4)'; ctx.shadowBlur = 24; ctx.shadowOffsetY = 10
                    ctx.drawImage(signature.cover, left, y + 10, coverW, coverH)
                    ctx.restore()
                }
                ctx.fillStyle = sepia; ctx.font = `600 26px ${serif}`; ctx.fillText('İMZA KİTABI', textLeft, y + 60)
                ctx.fillStyle = ink; ctx.font = `${story ? 48 : 42}px ${serif}`
                const lines = wrap(ctx, signature.title, right - textLeft, 2)
                lines.forEach((line, i) => ctx.fillText(line, textLeft, y + 120 + i * (story ? 58 : 50)))
                if (signature.author) {
                    ctx.fillStyle = '#6b4c33'; ctx.font = `italic 30px ${serif}`
                    ctx.fillText(signature.author, textLeft, y + 130 + lines.length * (story ? 58 : 50))
                }
            },
        })
    }

    // Take sections from most to least essential while they fit, then draw the chosen ones in reading order.
    const areaTop = top + 200, areaBottom = footerTop - 30, gap = story ? 48 : 34
    let used = -gap
    const chosen = new Set<Section>()
    for (const section of [...sections].sort((a, b) => a.priority - b.priority)) {
        if (section.priority > 0 && used + gap + section.height > areaBottom - areaTop) continue
        used += gap + section.height
        chosen.add(section)
    }
    let y = areaTop + Math.max(0, (areaBottom - areaTop - used) / 2)
    for (const section of sections.filter(s => chosen.has(s))) { section.draw(y); y += section.height + gap }
}

export async function renderTasteCard(profile: TasteProfile, nickname: string | null, format: CardFormat,
    signature: SignatureBook | null, linkProfile: boolean): Promise<Blob> {
    const canvas = document.createElement('canvas')
    drawTasteCard(canvas, profile, nickname, format, signature, linkProfile)
    return await new Promise<Blob>((resolve, reject) => canvas.toBlob(b => b ? resolve(b) : reject(new Error('empty')), 'image/png'))
}

/** Shares the card through the system share sheet when possible, otherwise downloads it. */
export async function shareCardImage(blob: Blob, nickname: string | null, linkProfile: boolean): Promise<'shared' | 'downloaded'> {
    const file = new File([blob], 'favo-okur-kimligi.png', { type: 'image/png' })
    if (navigator.canShare?.({ files: [file] })) {
        try {
            await navigator.share({ files: [file], title: 'Okur kimliğim',
                text: nickname && linkProfile ? `Okur kimliğim Favo'da: ${profileUrl(nickname)}` : 'Okur kimliğim Favo\'da' })
            return 'shared'
        } catch (error) {
            if (error instanceof DOMException && error.name === 'AbortError') return 'shared'
        }
    }
    downloadCardImage(blob)
    return 'downloaded'
}

export function downloadCardImage(blob: Blob) {
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = 'favo-okur-kimligi.png'
    // Attached to the document so every browser follows the download link.
    document.body.append(link)
    link.click()
    link.remove()
    setTimeout(() => URL.revokeObjectURL(link.href), 1000)
}
