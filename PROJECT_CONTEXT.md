# KitapKivo Project Context

Updated 2026-09-16 from the codebase. This file describes implementation status,
not a claim that the application has passed production or browser verification.

## Current implementation

The .NET 10 modular monolith contains Identity, Catalog, Reading, Profiles and
Community. All five modules now have implemented behavior; Community currently
contains ratings. React/TypeScript/Vite supplies the web frontend.

- Identity: registration, login, JWT, rotating refresh sessions and logout.
- Catalog: authors, books, editions, publishers, categories, local browse/search,
  details and batch summaries; Open Library discovery, resolution and import jobs.
- Reading: personal library add/list/remove, status changes, book lookup and summary.
  States: WantToRead, Reading, Read, OnHold, Dropped.
- Profiles: one bedside book and five influential positions with personal notes.
- Community: seven required 1-10 criterion scores, weighted overall, public criterion
  averages, create/update/remove and the user's detailed evaluation on book details.
- API: centralized exceptions, ProblemDetails, development Swagger and health.
- Frontend: local catalog, book details, login/register and protected personal profile.

Last committed feature before this continuation: `57f5578` (2026-08-27), requiring
Turkish editions for newly resolved external catalog books. Existing records are
reused before that validation; the change does not retrofit older catalog data.
External book absence and missing suitable Turkish edition currently share the
resolution NotFound outcome.

## Current continuation

Library addition and profile book selection now validate local BookId through
IBookCatalog before writing. Catalog implements the read-only contract; Reading
and Profiles do not access Catalog persistence. Missing books return HTTP 404
ProblemDetails. Existing profile selections survive rejected replacements.
See ADR 0004 for boundaries and limitations. No migration is required.

Catalog accepts only external books with a Turkish edition. A data migration clears
cover URLs from all existing non-Turkish editions while retaining their edition and
book rows. Browse, summary and detail responses also refuse to expose a non-Turkish
cover even before cleanup is applied. The migration was applied to the local
development database on 2026-09-16.

Community stores one unique evaluation per user/book, containing seven integer
scores from 1 to 10. Theme, language, characters, structure, impact and experience
each contribute 15%; pace contributes 10%. Overall is calculated server-side.
Criterion averages are calculated separately; no-vote averages are null. Legacy
1-5 votes are preserved but excluded from detailed averages. Both Community
migrations were applied locally on 2026-09-16. See ADR 0005.

At the user's explicit request, local catalog cleanup removed 154 of 257 books
without a covered Turkish edition on 2026-09-16. 103 books remain. Four library
entries, one bedside selection and one influential selection were removed with
their books. Recovery copies are in schema catalog_cleanup_20260916074514901.
The maintenance report then found zero remaining candidates. See the maintenance
tool README for the rule, execution and recovery. Cover existence means a nonblank
stored URL, not verification of the remote image. This one-time cleanup does not
change the existing import requirement: a Turkish edition is required, a cover is not.

Successful API integration scenarios now create actual catalog books rather than
using arbitrary GUIDs. Regression cases cover missing books and unchanged profile
selections. Verification for detailed ratings: 143 unit/architecture tests, 13
Community API tests, one migration preservation/constraint test and two frontend
calculation tests passed. Frontend build/lint passed. PostgreSQL tests ran in Docker.
The full API run passed 106/109 tests; three old foreign-cover expectations were
updated to the requested Turkish-cover rule, and all six tests in the affected
classes then passed. Browser smoke-check confirmed 103 catalog results, the guest
rating panel and its seven weights at a narrow viewport. Authenticated browser
E2E coverage remains pending; authenticated rating flows passed API tests.

## Product and data rules

Catalog expansion on 2026-09-16 added exactly 1,000 new books, bringing the local
total to 1,103. All 1,000 have a Turkish edition with a provider cover ID and author
metadata; verification found zero foreign-language new editions and zero duplicate
ISBN-13s involving new books. The explicit CatalogSeed tool reused normal Catalog
resolution with cached edition-specific Open Library search data. 319 candidates
were skipped and no persistence failures occurred. Source pages and created IDs
are in `artifacts/catalog-import-20260916`; see the tool README for import rules.
The book-detail "Kitap hakkında" section is temporarily hidden in the frontend;
existing database descriptions are retained. Build/lint and browser checks passed.

The web UI now uses a quieter reading-room palette with a darker warm background,
smaller covers and compact profile/book-detail controls. Detailed ratings occupy a
separate full-width section (two form columns on desktop, one on mobile). Catalog
has a category dropdown sourced from `/api/catalog/categories`, including counts,
URL-backed selection, reset-to-page-one behavior and retry on category load failure.
Path navigation has a brief spine-like rotation, disabled for reduced-motion users.
Frontend build/lint passed. Browser checks covered category filtering (Biyografi:
3 of 103 books), login, adding a book to the reading list, bedside selection and
desktop/390px profile and rating layouts. The disposable test account was removed.

- User data references internal BookId, never an external provider identifier.
- Work-level Book and language/publisher-specific Edition are distinct concepts.
- Open Library is implemented; Wikidata and Google Books are future additions.
- The web catalog currently reads local data, not the external search endpoint.
- No in-app reading or copyrighted book-file hosting.
- MVP social scope: ratings, reviews and review comments.
- Following, likes, feeds, notifications and a dedicated admin UI remain deferred.

## Next work, in order

### Current book-only recommendation groundwork

The profile now gently invites users to evaluate up to three recently completed,
unrated books. `GET /api/community/me/rating-prompts` requires authentication,
uses the caller's identity and disables caching. Community reads completed book
IDs through Reading's `ICompletedBooks` contract, excludes detailed votes before
limiting results and ignores missing catalog books. Legacy votes remain eligible.
The UI supports temporary dismissal, retry and refresh on window focus. Links
open and focus the book's seven-criterion form; returning after a saved rating
removes that book from the prompt list. No migration is needed.

Personalized book discovery v1 is now available on the signed-in home page and
profile. `GET/PUT /api/community/me/reading-preferences` stores up to five genre
slugs and three criterion keys per account. `GET /api/community/me/recommendations`
returns six books with factual reasons. All routes require authentication; reads
use no-store. Preferences persist in community.ReadingPreferences via migration
20260916122641_AddReadingPreferences (applied to local development).

Community consumes Catalog's IDiscoveryCatalog and Reading's IReadingHistory.
Only covered Turkish editions qualify. Read/Reading books and the user's detailed
votes are excluded; WantToRead books may appear. Own overall scores >=7 supply
genre/author similarity signals. Explicit preferences are separate from rating
scores. Criterion evidence needs at least three detailed votes; five neutral
prior votes moderate small samples. Empty histories use clearly labeled discovery
suggestions. This is deterministic content-based ranking, not collaborative
filtering or a calibrated probability. See docs/decisions/0006-book-recommendations.md.

Validation: frontend build/lint, 32 Community unit tests, 18 rating/discovery API
tests and seven architecture tests pass. Browser verification covered saving
Bilim Kurgu + Tempo, changed reasons, persistence after reload, and desktop/390px
layouts. The disposable account and its preferences were removed. Next product
steps: recommendation feedback and diversity, then repeatable browser E2E tests.
Film/series support remains deferred.

Recommendation feedback now persists per (UserId, BookId) in
community.RecommendationFeedback (migration 20260916132629, applied locally).
PUT/DELETE `/api/community/me/recommendations/{bookId}/feedback` sets interested,
dismissed, or clears feedback. Interest supplies a weaker +1 genre/author signal;
dismissal excludes only the exact book. It does not alter ratings or library
status. The recommendation response includes the caller's feedback history,
including unavailable catalog books, so undo remains available after navigation.
UI refresh preserves unsaved preference drafts. After a successful write, undo
controls remain usable even if recommendation refresh fails. Validation: 34
Community unit tests and 19 rating/discovery API tests passed; browser verified
interest, dismissal, replacement recommendations and undo on a disposable account.

### Existing engineering backlog

1. Exercise login, local book discovery, library changes, detailed evaluations
   and profile selections together in a browser; add repeatable E2E coverage.
2. Define authorization policies for catalog writes and bulk import jobs; currently
   these require authentication but do not have a dedicated management policy.
3. Add catalog sorting/filtering by weighted overall or a chosen criterion through
   a module contract/read model; order the full result set before pagination.
4. Connect external search/resolution to a user-facing discovery flow.
5. Distinguish missing Turkish editions from absent books and upstream failures;
   continue Community with reviews, then comments.
6. Add E2E tests, CI, production configuration, migrations/backup procedures and
   operational visibility before public launch.

See README for setup and ROADMAP for acceptance criteria. Preserve module
boundaries; do not add cross-module DbContext access or a generic repository.
