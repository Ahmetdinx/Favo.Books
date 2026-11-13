import { useEffect, useId, useLayoutEffect, useRef, useState, type ReactNode } from 'react'
import './Popover.css'

type Props = {
    /** Accessible name of the panel. */
    label: string
    trigger: ReactNode
    children: ReactNode
    className?: string
}

/**
 * Opens on hover (with a short grace period to move into the panel), on keyboard focus and on click/tap.
 * A click pins it open until Escape, a second click or a click outside.
 */
export default function Popover({ label, trigger, children, className = '' }: Props) {
    const id = useId()
    const root = useRef<HTMLDivElement>(null)
    const panel = useRef<HTMLDivElement>(null)
    const closeTimer = useRef<number | undefined>(undefined)
    const [open, setOpen] = useState(false)
    const [pinned, setPinned] = useState(false)
    const [shift, setShift] = useState(0)

    const show = () => { window.clearTimeout(closeTimer.current); setOpen(true) }
    const hideSoon = () => {
        if (pinned) return
        window.clearTimeout(closeTimer.current)
        closeTimer.current = window.setTimeout(() => setOpen(false), 160)
    }
    const close = () => { setOpen(false); setPinned(false) }

    useEffect(() => () => window.clearTimeout(closeTimer.current), [])
    useEffect(() => {
        if (!open) return
        const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') close() }
        // Clicks inside a modal dialog opened from the panel (top layer, outside this DOM subtree) are not outside clicks.
        const onPointer = (e: PointerEvent) => {
            if ((e.target as Element | null)?.closest?.('dialog[open]')) return
            if (!root.current?.contains(e.target as Node)) close()
        }
        document.addEventListener('keydown', onKey)
        document.addEventListener('pointerdown', onPointer)
        return () => { document.removeEventListener('keydown', onKey); document.removeEventListener('pointerdown', onPointer) }
    }, [open])

    // Keep the panel inside the viewport with a 12px margin.
    useLayoutEffect(() => {
        if (!open || !panel.current) { setShift(0); return }
        const rect = panel.current.getBoundingClientRect()
        const left = rect.left - shift
        const overflowRight = left + rect.width - (window.innerWidth - 12)
        setShift(overflowRight > 0 ? -Math.min(overflowRight, left - 12) : left < 12 ? 12 - left : 0)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open])

    return <div ref={root} className={'popover ' + className} onMouseEnter={show} onMouseLeave={hideSoon}
        onFocus={show} onBlur={e => { if (!root.current?.contains(e.relatedTarget as Node)) hideSoon() }}>
        <button type="button" className="popover-trigger" aria-expanded={open} aria-controls={id}
            onClick={() => { if (pinned) close(); else { setPinned(true); show() } }}>
            {trigger}
        </button>
        {open && <div ref={panel} id={id} role="dialog" aria-label={label} className="popover-panel"
            style={shift ? { transform: `translateX(${shift}px)` } : undefined}>
            {children}
        </div>}
    </div>
}
