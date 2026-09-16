# KitapKivo Roadmap

Updated 2026-09-16. Checked items denote implementation in code, not production
certification. Deliver each feature with its API behavior, relevant tests and UI.

## Implemented foundation

- [x] .NET 10 solution, module registration and project dependency tests.
- [x] PostgreSQL persistence and migrations for Identity, Catalog, Reading, Profiles.
- [x] Central exception handling and ProblemDetails; Swagger and health endpoint.
- [x] Registration, JWT authentication, refresh sessions and logout.
- [x] Local catalog models, creation, browse/search, categories, details and summaries.
- [x] Open Library client abstractions, search, details and local record resolution.
- [x] Background import jobs, retry behavior and outgoing Open Library request gate.
- [x] Turkish-edition requirement for newly resolved external books.
- [x] Personal library and five reading states, ownership checks and summary.
- [x] Profile bedside book and influential book selections.
- [x] Keep external imports Turkish-edition-only and clear non-Turkish cover URLs.
- [x] Add seven-criterion 1-10 ratings, weighted overall and book-detail UI.
- [x] Clean local books without a covered Turkish edition, with recovery snapshots.
- [x] Web authentication, local catalog, book details and private profile.
- [x] Validate Catalog book existence before Reading/Profiles book writes.
- [x] Update project context and onboarding documentation.

## Milestone 1: Trust the existing user journey

- [x] Run the PostgreSQL integration suite; fix old foreign-cover expectations and rerun affected tests.
- [ ] Align development and integration PostgreSQL versions.
- [ ] Verify clean setup and all five persistent module migrations from the README.
- [ ] Browser-check login, refresh, logout, catalog, library and profile selections.
- [ ] Add E2E coverage for the core user journey.
- [ ] Verify concurrent duplicate writes produce controlled responses.
- [ ] Define catalog write/import authorization policies.

Acceptance: a new developer can start the app; users can manage real catalog books
without dangling references; missing books do not overwrite profile selections.

## Milestone 2: Complete book discovery

- [ ] Separate missing book, missing Turkish edition and upstream failure outcomes.
- [ ] Measure metadata gaps and older records affected by the Turkish-edition policy.
- [ ] Connect external search/details/resolution to the web discovery flow.
- [ ] Cover empty results, missing metadata, retry and cancellation experiences.

Acceptance: a supported book absent from the local catalog can be discovered and
added by the user without developer intervention, with understandable failure states.

## Milestone 3: Community MVP

- [x] Seven independent criterion scores, specified weights, criterion averages and legacy-vote migration.
- [x] Private profile reminders for up to three read books without detailed ratings, with direct links to the rating form and optional dismissal.
- [x] Capture reading preferences and deliver explainable personalized book recommendations; distinguish criterion importance from a book's criterion scores.
- [x] Persist recommendation interest/dismissal feedback with undo and account isolation.
- [ ] Add recommendation diversity; measure quality and replace full in-memory catalog/rating scans with a bounded read model before scaling.
- [ ] Catalog sorting/filtering by overall or selected criterion, before pagination.
- [ ] Publish/edit/delete reviews, ownership, pagination and spoiler behavior.
- [ ] Add/edit/delete review comments with ownership checks.
- [ ] Connect all three flows to book details and add domain/API tests.

Acceptance: two users can rate, review and discuss a book without modifying each
other's content. Following, likes and activity feeds are not required.

## Milestone 4: Public release readiness

- [ ] CI for backend tests and frontend build/lint.
- [ ] Production secrets, deployment and database migration strategy.
- [ ] Backup and restore verification, operational logs and import job visibility.
- [ ] Incoming request limits, account recovery decision and security checks.
- [ ] Responsive layout, accessibility and critical browser E2E checks.
- [ ] Deploy and verify the first public MVP.

## Deferred candidates

Wikidata enrichment, Google Books fallback, agreements for Turkish edition data,
following, likes, feeds, notifications and dedicated admin UI. Film/series support
remains outside the current book-only scope.
Measure data gaps before adding commercial providers. Do not scrape Milli Kutuphane.
