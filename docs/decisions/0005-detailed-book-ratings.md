# ADR 0005: Seven-criterion book evaluations

Accepted 2026-09-16.

## Decision

Each authenticated user submits all seven integer scores from 1 to 10 for a book.
The unique (UserId, BookId) constraint makes a repeat submission an update.

| Key | Criterion | Weight |
| --- | --- | --- |
| theme | Konu / Fikir | 15% |
| language | Anlatım ve Dil | 15% |
| characters | Karakterler | 15% |
| structure | Kurgu ve Yapı | 15% |
| pace | Tempo / Sürükleyicilik | 10% |
| impact | Duygusal / Düşünsel Etki | 15% |
| experience | Genel Okuma Deneyimi | 15% |

Overall = sum(score * weight) / 100, on the same 1-10 scale. The server calculates
it; client input cannot override it. Domain, endpoint and PostgreSQL constraints
enforce valid complete evaluations. A PostgreSQL upsert handles concurrent first
submissions without duplicate votes, preserving identity and creation time.

GET `/api/community/books/{bookId}/ratings` returns `averageScore`, `ratingCount`,
`criteriaAverages`, `criteria` metadata and optional `currentUserRating` with
`scores` and `overallScore`. PUT `/rating` accepts the seven flat criterion keys;
DELETE `/rating` removes only the authenticated user's evaluation. Reads and
submissions validate Catalog existence through IBookCatalog. GET uses no-store because the
response can include the caller's evaluation. Averages are calculated in SQL and
remain unrounded until display. No evaluations means null averages, never zero.

## Migration

The previous Score column becomes LegacyScore. Existing votes keep their original
value, identity and timestamps, with null criterion fields. Legacy votes do not
contribute to detailed averages or ratingCount; legacyRatingCount and the caller's
currentUserLegacyScore expose the transition honestly. Updating a legacy vote
replaces it with a complete evaluation. Downgrade is blocked while detailed votes
exist, preventing silent loss of their seven scores.

## Later catalog discovery

Criterion persistence and averages support future ranking; catalog sorting and
filtering are not implemented in this change. Introduce an explicit shared read
contract or maintained projection, never a Catalog dependency on Community's
DbContext. Apply chosen criterion/overall sorting across the entire matching set
before pagination, with stable tie-breaks, null handling and displayed vote counts.
Define minimum-vote behavior before treating a one-vote average as a recommendation.
