# KitapKivo Project Context

Updated 2026-09-29. This file describes implementation status,
not a claim that the application has passed production or browser verification.

Latest checkpoint (2026-09-29, second requested list): completed 200 request rows /
194 distinct works. Imported 132 new books, unhid 38 existing records, preserved
24 already-visible works. All 62 existing identities retained. Total records
10,915 -> 11,047; visible catalog 1,100 -> 1,270. Source hashes and 194 cover image
headers/dimensions verified; actual BrowseBooksHandler found 194/194 works.
Same-manifest replay created zero books and passed all 194 search checks again.
Eight focused import/curation tests pass. Three older editions have no ISBN
listed in their sources; no identifier fabricated. Nine explicitly requested
short works/collections/nonfiction narratives use SelectedClassic. See
docs/reading-list-200-20260929.md and its linked 200-row results report.
No commit, push or production deployment.

Previous checkpoint (2026-09-28, requested 100-book list): all 100 requested works
are visible and found by the actual catalog search handler. Imported 50 new books;
preserved 50 existing BookIds, including their library/rating references. Total
records 10,865 -> 10,915; visible catalog 1,012 -> 1,100. Exact-manifest source
hashes and all 100 edition cover image headers/dimensions verified. Replay created
zero duplicates and again passed 100 search checks. Seven import/curation API
tests pass, including adding an edition to an existing tracked book. Offline
ReadingListImport supports explicit identity mappings, atomic writes, ISBN/source
collision checks and content-bound replay. Migration
20260928113705_AddSelectedClassicDecision applied locally: six explicitly requested
non-novel classics use SelectedClassic, not a false Novel classification. Unknown
books remain hidden. Kartal Kondu's 1978 Turkish edition has no assigned ISBN;
Kizil Koskun Ruyasi's verified Turkish edition is volume 1. See
docs/reading-list-100-20260928.md and data/curation/reading-list-100-20260928 for
the complete per-book manifest/results. No production deployment or push.

Previous checkpoint (2026-09-28, Google sign-in): optional GIS popup button on login
and registration; Google:ClientId enables it. JWT signature/issuer/audience/lifetime,
verified email and one-use browser nonce are validated server-side. New private
users use Google sub identity and choose nickname on profile; matching local email
is never auto-linked. Existing token/refresh-cookie sessions reused. Identity
migration 20260928063347_AddGoogleIdentity applied locally. 30 Identity unit tests,
two isolated PostgreSQL Google flow tests and frontend build/lint passed. Real
Google/browser verification awaits user-created Web Client ID. Setup in
 docs/google-sign-in.md. In-memory challenge store requires shared atomic storage
or sticky routing before multi-instance deployment.

Previous checkpoint (2026-09-25, classic coverage): imported seven source-verified
Turkish-edition classics, batch classics-20260925. Eligible novels 1,005 -> 1,012;
total records 10,858 -> 10,865. Repeat preview reports zero new and unchanged counts.
Manifest/verification/apply/replay reports: data/curation/classics-20260925.
Existing unreviewed classics were identified but not promoted; work-ID audit access
was declined. See docs/classic-coverage-20260925.md for remaining gaps. Collector
handles the publisher's malformed JSON-LD via complete Product-object recovery;
four extractor tests pass. No user data changed.

Previous checkpoint (2026-09-25, celebration and catalog priority): dismissible,
reduced-motion-aware achievement toast shows avatar and next milestone. Per-account
browser-local high-water marks avoid repeat celebrations; first visit establishes
a baseline. Catalog authenticates browse calls, bypasses shared client cache for
personal results, and demotes Read/Reading/WantToRead IDs before pagination via
Reading contract. Existing sort is retained within each group; entries remain
searchable. Library changes refresh ordering. API response is private/no-store.
Four PostgreSQL browse integration tests and four frontend catalog tests passed;
Release API build and frontend build/lint passed. Visual browser QA pending.

Previous checkpoint (2026-09-25, own-library discovery): authenticated no-store
GET /api/community/me/recommendations/library ranks up to three eligible WantToRead
novels via Reading's user-scoped contract. Ranking retains full liked-book context,
allows other genres in one's own library, ignores interest exclusion for this shelf,
and excludes already rated books. Home cards explain reasons and offer Start reading;
library, preference and rating changes refresh the shelf. No schema changes.
38 Community unit tests, frontend build/lint passed; browser and API integration
verification pending. Development preview toggle remains available as requested.

Previous checkpoint (2026-09-25, recommendation preview): interested feedback now
excludes the selected novel while remaining a taste signal for related novels.
Frontend removes it after successful feedback and reloads replacement candidates.
Development-only UI toggle opens an in-memory fixture preview, ranked by the real
backend algorithm. Authenticated preview endpoint returns 404 outside Development;
no fixture data is persisted, and preview actions never write library/feedback.
Includes perfect/cross-genre shelves, exact-9 and two-vote boundary examples,
replacement candidates and reset. Use the home-page "Test verileriyle önerileri dene"
button with Vite dev and API Development. 36 Community tests, eight frontend library
tests, build/lint passed. Browser interaction verification remains pending.

Previous checkpoint (2026-09-25, recommendation shelves): perfect matches require
an explicit genre match, exactly three chosen criteria each averaging strictly
above 9, and at least three complete ratings. Separate cross-genre discovery
requires all chosen criteria above 9 with the same evidence minimum and no chosen
genre overlap. Personal shelf stays within selected genres when supplied. Limits
are 6 perfect / 6 personal / 3 discovery; no duplicates or fabricated percentages.
Cards expose genre, criterion averages and vote counts. Existing read/rated/dismissed
exclusions remain. Full catalog/rating reads remain a scaling task. Community unit
tests (35), frontend build/lint passed; browser/API integration QA remains pending.

Previous checkpoint (2026-09-25, rating UX): criterion prompts and score labels are
shorter, community averages are expandable, and progress shows answered criteria.
Partial drafts persist locally per account/book with validation and storage-failure
feedback. Restored drafts can be discarded; successful submission/removal clears
them. Drafts never enter server ratings, recommendations or achievements. This is
browser-local persistence, not cross-device sync. Submission shows a brief thank-you.
Frontend build/lint and six rating/draft tests passed; browser QA remains pending.

Previous checkpoint (2026-09-25, discovery UX): recommendation interest first ensures
a WantToRead library entry, preserving any existing status and handling concurrent
adds. Feedback removal does not delete library entries. Library and feedback writes
are separate requests; partial failures are surfaced and retry is safe. Home copy
is shorter with expandable recommendation explanations; guest hero focuses on novels.
Featured profile panels stretch to equal height on desktop, including empty states.
Interface action capitalization is consistent. Frontend build/lint and eight
recommendation-library tests passed. Browser visual verification remains pending.

Previous checkpoint (2026-09-24, avatars): eight preset reader avatars persist in
Identity (default owl); migration 20260924132516_AddReaderAvatar applied locally.
Profile settings offer radio selection and a preview. The header Profile link is
now the selected avatar, with reading-tier frame/left badge and independent
evaluation-tier right badge, derived only from server achievement data. Public
profiles reuse the same presentation behind the visibility gate. No uploads or
client-selected achievement tiers. Old settings clients may omit AvatarId without
resetting it. Eleven reader/achievement API tests passed; browser visual QA remains
pending. Header badges refresh after library/rating changes and on window focus.

Previous checkpoint (2026-09-24, achievements): separate reading and completed-
evaluation badges at 1/10/100/1000 eligible novels. Counts use distinct work IDs,
current Read state and complete seven-criterion ratings; legacy scores and
ineligible catalog records are excluded. Catalog owns eligibility via a shared
contract with 500-ID batches; no cross-module table access. Counts and badges
recompute after changes (no permanent award history or fabricated award dates).
Profile displays two compact progress cards and next goals; public profiles
expose badges under the existing visibility gate. No schema migration required.
Nine targeted API tests, 22 Profiles unit tests, seven architecture tests,
frontend build/lint and API Release build passed. Browser E2E remains pending.
User explicitly deferred bulk library addition; achievements are the active step.

Previous checkpoint (2026-09-24, catalog-to-library): catalog cards now expose
WantToRead/Reading/Read with existing status, per-book pending/error handling and
undo. Authenticated status lookup is bounded to 50 IDs (one request for the
visible page), user-scoped and no-store. Undo uses an atomic owner+timestamp
comparison so newer changes are not overwritten; adding returns UpdatedAtUtc.
Concurrent duplicate adds return conflict instead of 500. Login/registration
preserve catalog filters and the intended book/status for explicit continuation.
Marking Read does not require rating; a quiet optional detail link is shown.
29 Reading API tests, frontend build and lint passed. Browser E2E remains
pending. Bulk selection and partial rating/drafts are separate next steps.

Previous checkpoint (2026-09-24): nickname and opt-in public profiles implemented.
New registration requires a unique lowercase nickname; existing accounts choose
one from profile settings. All accounts default private. Migration
20260924065516_AddReaderIdentity applied locally, preserving existing libraries.
Public nickname search is paginated; the header search displays up to five
public matches while typing and /okur/:nickname opens shared libraries.
The former /okurlar page redirects home. Catalog search updates after a 300 ms
debounce, preserving filters and resetting pagination. The shared theme uses
warm layered gradients, subtle CSS paper grain and quiet ivory surfaces.
The host composes module-owned read
models; emails and personal notes/reflections are never projected publicly.
Reader APIs use no-store and return 404 for missing/private profiles.
141 API tests, seven architecture tests, frontend build and lint passed.
Browser E2E remains pending. See docs/reader-profiles.md for contracts and limits.

Previous checkpoint (2026-09-23): exactly 1,000 NEW covered Turkish-edition novels
imported in batch publisher-novels-20260918. Total books: 10,858; eligible catalog:
1,005. Existing records and user history were preserved. Final preview found
1,001 new works and 227 existing/duplicate candidates in a 1,228-record verified
manifest. Apply selected 1,000 distinct book IDs; repeat preview recognized the
same batch, returned zero new works and unchanged totals. Source facts, verification
hash and applied selection are under data/curation/publisher-novels-20260923.
Editorial exceptions/holds are source-hash-bound in
data/curation/publisher-form-reviews-v1.json. Doğan original-language defaults are
not trusted; unknown is stored. Catalog unit tests: 112 passed; Release importer
build and Python syntax checks passed. Earlier 134 API integration tests and four
frontend catalog tests passed before the latest identity changes; no fresh Docker
test result is claimed. See docs/publisher-novel-import-2026-09-23.md and
tools/KitapKivo.PublisherImport/README.md. The API was not listening on port 5117;
post-import eligible counts were verified directly through the Catalog query.

Previous checkpoint (2026-09-17): novel eligibility now gates catalog browsing, recommendations
and genre counts. The category API exposes 11 novel genres; seven new categories
were added by migration 20260917171539_AddNovelGenres, applied locally. Unknown
records and user history are retained. Maintenance supports
`report|apply --confirmed-non-novels`, backing up reviews and recommendation
feedback as well as existing related tables. Local report: 9,858 books, zero
confirmed NotNovel deletion candidates, five reviewed novels. No deletion and
no new imports performed. The requested 1,000 verified new novels are unfinished;
publisher HTML research in artifacts/novel-publisher-import is not approval data.
Validation: 130 API integration tests, 98 Catalog unit tests and frontend build
passed. Existing broad subject categories were retained internally; history and
psychology subjects are not automatically converted into novel subgenres.

## Authoritative product revision (2026-09-17)

See docs/novel-product-plan.md and ROADMAP.md. The product is now novel-only
discovery with virtual libraries, seven-criterion evaluations, achievements and
opt-in public profiles searchable by nickname. No posts, messages, review/comment
threads or shared social feed. Covered Turkish editions remain required; popular
classics take import priority. The requested 1,000-new-novel import is complete.
Novel classification, central eligibility and nickname/privacy are implemented;
achievements remain planned. Next: the catalog-to-library user experience.
Do not infer novels from `fiction` alone
or delete historical user records. Keep percentage-free recommendations until a
versioned match index and evidence policy are validated. Earlier social-scope
statements are superseded by this revision.

## Current implementation

Novel import enforcement checkpoint (2026-09-17): new records now require a
source-reviewed entry in embedded data/curation/novel-import-priorities-v1.json,
matching provider/work title/author and a covered matching Turkish edition.
ResolveExternalBookHandler also gates existing reuse by latest review and cover.
CreateBookHandler's legacy free-form write path is closed (422 for valid requests).
API, batch and background resolver paths share enforcement; rejections have codes.
CatalogSeed now previews by default and applies only the ordered curated list with
--apply; no random fallback or old broad-import resume. No new books imported.
129 API integration tests and 90 Catalog unit tests passed. Five-item preview
verified without database/network/files. See docs/novel-import-policy.md.
Public browse/recommendation visibility remains unchanged; expand curated coverage
and implement archived-history transition before enabling the read-side filter.
The list is editorial classic priority, not measured readership ranking.

Editorial eligibility checkpoint (2026-09-17): migration
20260917123207_AddNovelReviews applied locally; append-only review workflow and
central SQL eligibility query implemented. tools/KitapKivo.CatalogCuration checks
work/title/author identity and applies atomic idempotent batches. Five reviews from
data/curation/classic-novels-v1.json applied after preview. 90 Catalog unit tests
and three new PostgreSQL integration tests passed. See ADR 0007 for sources.
Public routes/imports are NOT gated yet: the five-book seed is too small and the
private-history transition remains outstanding. No books or user data deleted.
Next: expand curated coverage (including Turkish classics), then wire eligibility
consistently across discovery/read/write paths and preserve private archived history.

Novel audit checkpoint (2026-09-17): Catalog's NovelEvidenceClassifier v1 and
tools/KitapKivo.CatalogAudit provide read-only source-label triage. Local report:
9,858 books; 84 NovelCandidate, 961 NonNovelCandidate, 10 Conflict, 8,803 Unknown.
9,857 have cached source observations; all have a stored covered Turkish edition.
These are NOT verified forms; no database writes, eligibility filter or imports
were performed. 83 Catalog unit tests passed, including 19 new cases; audit
Release build and actual read-only database run passed. See
docs/catalog-novel-audit.md for evidence limitations and artifacts paths.
Next: verify a curated classics seed with source evidence, then persist editorial
eligibility and activate consistent catalog/import filtering with history preservation.

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
- MVP scope: novel recommendations, ratings, achievements and opt-in public libraries.
- Reviews, comments, posts, messages and follow feeds are excluded.

## Next work, in order

Catalog performance update (2026-09-17): medium Open Library cover URLs in cards,
first-two-image priority, async decoding/preconnect, bounded 60-second page/category
caches, split metadata queries and deterministic paging indexes. Migration
20260917055222_OptimizeCatalogPaging applied locally. Frontend build/lint, three
catalog tests and two API tests passed, including an isolated 100k-book fixture.
Warm first-page measurements were 13–14 ms; one cover sample shrank 63.5% in bytes.
See docs/catalog-performance.md for methodology and remaining scaling limits.

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
   follow the revised novel product plan rather than adding reviews or comments.
6. Add E2E tests, CI, production configuration, migrations/backup procedures and
   operational visibility before public launch.

See README for setup and ROADMAP for acceptance criteria. Preserve module
boundaries; do not add cross-module DbContext access or a generic repository.

### Catalog test-data checkpoint (2026-09-17)

The user stopped the 10,000-book import because the current dataset is sufficient.
Do not resume importing unless explicitly requested. The importer was terminated.
Verified local catalog total: 9,858 books. The run manifest contains 8,754 distinct
book IDs, all present in the database, with Turkish-edition cover URLs and authors;
no foreign editions or duplicate ISBN-13 values were found in this manifest batch.
Cover URL metadata was checked, not every remote image download.
Cached source pages and created.jsonl remain in artifacts/catalog-import-20260917;
the last manifest entry is on page 298. verification.json records the checks.
The total exceeds the previously recorded baseline (1,103) plus manifest count by
one. The latest non-manifest book is 6b0510c3-c2e2-4d95-ba34-c863355734b9
(Taş Can ve Kutsal, created 2026-09-17T06:25:01.340Z). Its provenance has not been
reconciled; do not assume the manifest identifies every addition or use it as a
complete rollback inventory. Existing records were preserved.
Catalog performance and seed-tool limit changes were committed and pushed in a5aab73.
The product revision is a subsequent documentation change.
Continue development/testing with this dataset; no further import is pending.
