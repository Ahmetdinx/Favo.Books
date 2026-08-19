export type CatalogSearchField =
    | 'all'
    | 'title'
    | 'author'

export type CatalogBookSort =
    | 'recently-added'
    | 'title'
    | 'first-published-newest'

export type BrowseBookAuthor = {
    authorId: string
    name: string
}

export type BrowseBookCategory = {
    categoryId: string
    name: string
    slug: string
}

export type BrowseBookItem = {
    bookId: string
    title: string
    firstPublishedYear: number | null
    coverImageUrl: string | null
    authors: BrowseBookAuthor[]
    categories: BrowseBookCategory[]
}

export type BrowseBooksResponse = {
    totalCount: number
    page: number
    pageSize: number
    totalPages: number
    items: BrowseBookItem[]
}

export type BrowseBooksRequest = {
    query: string
    searchField: CatalogSearchField
    category: string
    page: number
    pageSize: number
    sort: CatalogBookSort
}

export type BookDetailsAuthor = {
    authorId: string
    name: string
    displayOrder: number
}

export type BookDetailsCategory = {
    categoryId: string
    name: string
    slug: string
}

export type BookEditionPublisher = {
    publisherId: string
    name: string
}

export type BookEdition = {
    editionId: string
    title: string
    languageCode: string
    format: string
    isbn10: string | null
    isbn13: string | null
    pageCount: number | null
    publicationYear: number | null
    coverImageUrl: string | null
    publisher: BookEditionPublisher | null
}

export type BookDetailsResponse = {
    bookId: string
    title: string
    originalLanguageCode: string
    originalTitle: string | null
    description: string | null
    firstPublishedYear: number | null
    primaryEditionId: string | null
    primaryCoverImageUrl: string | null
    authors: BookDetailsAuthor[]
    categories: BookDetailsCategory[]
    editions: BookEdition[]
}