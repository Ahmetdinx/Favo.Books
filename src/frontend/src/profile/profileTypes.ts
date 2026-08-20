export type BedsideBookSelection = {
    bedsideBookId: string
    bookId: string
    personalNote: string | null
    createdAtUtc: string
    updatedAtUtc: string
}

export type SetBedsideBookRequest = {
    bookId: string
    personalNote: string | null
}

export type SetBedsideBookResponse =
    BedsideBookSelection

export type InfluentialBookSelection = {
    influentialBookId: string
    bookId: string
    position: number
    personalReflection: string | null
    createdAtUtc: string
    updatedAtUtc: string
}

export type GetInfluentialBooksResponse = {
    items: InfluentialBookSelection[]
}

export type SetInfluentialBookRequest = {
    bookId: string
    personalReflection: string | null
}

export type SetInfluentialBookResponse =
    InfluentialBookSelection