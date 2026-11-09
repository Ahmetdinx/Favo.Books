const editionSuffix = /\s*(?:\((?:karton kapak|ciltli|ciltsiz|sert kapak|özel baskı)\)|[–-]\s*(?:\d+\s*cilt|ciltli|ciltsiz|sert kapak|karton kapak|özel baskı))\s*$/iu

export function cleanBookTitle(title: string) {
    return title.replace(editionSuffix, '').replace(/^"+|"+$/g, '').trim() || title
}
