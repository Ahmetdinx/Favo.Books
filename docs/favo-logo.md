# Favo Kitap logo asset

Approved direction: the media-neutral, interlocking open-heart Favo emblem.
The book application uses an espresso-brown version with a separate HTML wordmark
and Kitap label. Other applications may adapt their emblem/theme independently.

Asset: `src/frontend/public/brand/favo-mark.png`.
Integration: `FavoBrand.tsx` and its stylesheet, header, login, registration,
favicon and Apple touch icon. The old CSS-generated K badge was removed.
The wordmark is live text, not a crop of the concept board.

Generated with the built-in image_gen tool using the approved concept as a
reference. The first extraction had a glow and was discarded. Final edit prompt:

> Correct the extracted logo asset. The SECOND most recent image is the approved Favo logo board; use its exact upper-left heart emblem. The MOST recent extracted brown logo is defective: it has a huge blurry brown glow and dark fog around it. REMOVE ALL glow, fog, shadows, gradients, textures and background. Make ONLY a crisp SOLID fully opaque dark-brown #4A3024 silhouette of the original two interlocking heart strokes. Everything outside the strokes and all internal cutout space must be 100% transparent (alpha 0). The emblem must have fully opaque flat fill alpha255, not translucent. Tight square framing with 4% margin. No lettering. Preserve original approved shape exactly. This is a flat UI icon PNG, NOT a rendering, not a lighting effect.

Validation: frontend production build and lint pass. Login page inspected in the
browser: transparent emblem renders correctly on both header and form backgrounds.
No authentication or database behavior changed. Not committed or pushed.
