# KitapKivo Roadmap

Revised 2026-09-17. Authoritative scope and acceptance criteria:
[Novel discovery product plan](docs/novel-product-plan.md).
Checked items indicate implementation, not production certification.

## Foundation retained

- [x] Modular API, PostgreSQL migrations and React frontend.
- [x] Authentication, personal library and featured profile books.
- [x] Seven-criterion ratings, preferences and explainable recommendations.
- [x] Recommendation feedback, catalog performance improvements and 100k paging test.
- [x] Novel-only product plan; local dataset 10,858 books, 1,005 eligible novels.
- [x] Import 1,000 new covered Turkish-edition novels with source reviews, duplicate checks and repeat-batch verification (2026-09-23).

## 1. Novel catalog and eligibility

- [x] Model editorial literary-form decisions, evidence and review history (migration applied locally).
- [x] Initial read-only evidence audit of existing records; see docs/catalog-novel-audit.md.
- [ ] Review/verify actual literary form; audit candidates are not publication decisions.
- [x] First five source-backed world classics curated and applied; see ADR 0007.
- [ ] Expand verified Turkish/world classics coverage; no measured popularity claim yet.
- [ ] Apply eligibility to catalog, details, categories, recommendations and additions.
- [x] Enforce reviewed novel + covered Turkish edition on new-book write/import paths; close unverified free-form creation.
- [x] Replace broad seed search with ordered curated list, preview default and no random fallback.
- [ ] Preserve ineligible historical library entries privately as archived records.
- [ ] Separate literary form from subject; historical novels qualify, history research does not.
- [ ] Work-level deduplication and curated import priorities; no 100k launch quota.

## 2. Effortless catalog-to-library journey

- [x] Catalog card reading actions and guarded undo for the last visible-card change.
- [ ] Optional bulk past-reading addition — deferred by user; not the next step.
- [x] Preserve selected book/action and catalog filters through login/registration; explicit continuation.
- [x] Optional seven-criterion form with browser-local account/book drafts and no fabricated defaults.
- [ ] Mobile, keyboard, loading, empty and failure experiences.
- [ ] Measure first useful recommendation and recommendation-to-library conversion.
- [ ] Observe five readers completing the journey and fix obstacles.

## 3. Nicknames and profile sharing

- [x] Unique normalized nicknames, reserved names and existing-account migration.
- [x] Retain private authentication credentials; no required real name.
- [x] Private-by-default visibility with explicit sharing choice and field disclosure.
- [x] Public profile URL and paginated public-only nickname search.
- [x] Public library/evaluation contracts without cross-module database access.
- [x] API privacy tests: direct access, no-store, search, visibility transitions.
- [x] Existing personal notes remain private; public-to-private transition tests.
- [ ] Browser E2E and privacy tests for future rankings.

## 4. Achievements

- [x] Separate reading/evaluation badges at 1, 10, 100 and 1,000 eligible novels.
- [x] Recomputable counts; edits/re-adds do not inflate progress; removal is handled.
- [x] Compact profile tracks, next milestone and privacy-gated public badges.
- [ ] Quiet celebration, next milestone and reduced-motion support.
- [ ] No vote-weight privilege, speed rewards or forced streaks.
- [ ] Optional public opt-in ranking after metric/abuse rules; may ship later.

## 5. Personal discovery home

- [ ] Preference-based start without history; no fabricated match claims.
- [ ] Bounded candidate/read models replacing full catalog/rating scans.
- [ ] Negative signals, diversity, reading-state policy and unique shelf items.
- [ ] Separate global rating, personal match index and evidence sufficiency.
- [ ] Validate versioned 60/80/95 bands; suppress numbers with insufficient evidence.
- [ ] Do not force-fill empty recommendation bands.
- [ ] Overall/criterion sorting before pagination, vote counts and sparse-data handling.
- [ ] Beta feedback and later reading outcomes for quality measurement.

## 6. Release readiness throughout development

- [ ] Early staging; CI tests/build/lint; align PostgreSQL test/development versions.
- [ ] Fresh migrations/setup, catalog management authorization and request limits.
- [ ] Session security, recovery, verification policy, privacy and account deletion.
- [ ] E2E account/library/rating/recommendation/public-private profile journeys.
- [ ] Concurrent load, accessibility and mobile checks.
- [ ] Production secrets, data transfer, migration/rollback and restore rehearsal.
- [ ] Database readiness, error/availability/cost monitoring and support channel.
- [ ] Closed beta and controlled public beta around 2026-11-17.

## Explicit exclusions

Posts, messages, chat, review/comment threads, follow feeds, universal recommendation
homepage, film/series and in-app reading. Additional providers, native apps and
advanced rankings await evidence of useful discovery and repeat use.

See the product plan for the eight-week sequence and scope-reduction rules.
