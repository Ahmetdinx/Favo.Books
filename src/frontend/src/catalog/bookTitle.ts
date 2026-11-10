// Edition notes that publishers append to a title: binding, format, pocket size, modernised text.
const formatNote = /\s*\((?:[^)]*\b(?:ciltli|ciltsiz|karton kapak|sert kapak|bez cilt|özel baskı|cep boy|cep|günümüz türkçesiyle)\b[^)]*|\d+\s*cilt)\)\s*$/iu
// Volume numbers of a collected-works series: "Yaban Bütün Eserleri 1", "Kan Konuşmaz / Romanlar-1".
const collectedWorks = /\s*\/?\s*(?:bütün eserleri|toplu eserleri|romanlar)\s*[-–]?\s*\d+\s*$/iu
const dashedSuffix = /\s*[–-]\s*(?:\d+\s*cilt|ciltli|ciltsiz|sert kapak|karton kapak|özel baskı)\s*$/iu

export function cleanBookTitle(title: string) {
    let clean = title.replace(/^"+|"+$/g, '').trim()
    for (let previous = ''; previous !== clean;) {
        previous = clean
        clean = clean.replace(formatNote, '').replace(collectedWorks, '').replace(dashedSuffix, '').trim()
    }
    return clean || title
}
