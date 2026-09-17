# Catalog performance — 2026-09-17

## Changes

- Catalog remains paged at 12 books in the UI (API maximum 50).
- Recognized Open Library book-cover URLs use the M variant in catalog cards,
  preserving the exact edition/cover ID. Other providers and detail images remain
  unchanged. The first two images use eager/high-priority loading, the rest lazy;
  all use async decoding and explicit dimensions. A preconnect warms the cover host.
- Public catalog JSON has a session-only 60-second LRU cache, at most 20 pages.
  Category metadata has a one-entry cache with the same TTL. All query parameters
  are part of the page key. Errors and aborted responses are not cached; retries
  bypass the cached page. Newly imported records may take up to 60 seconds to show
  on revisiting an already cached page. Reload starts a fresh session cache.
- Browse uses split queries for author/category collections to avoid their
  Cartesian multiplication. Every sort ends in BookId for stable page boundaries.
- Migration 20260917055222_OptimizeCatalogPaging adds indexes matching recent/title
  ordering, replacing the redundant single-column Title index. Applied locally.

## Verification and observed measurements

- Frontend build/lint and three catalog tests pass: URL handling, bounded cache
  eviction/expiry, filter isolation, real fetch reuse, cancellation and error retry.
- Existing Turkish-edition browse test and new CatalogScaleTests pass.
- Scale test uses an isolated PostgreSQL 17 Testcontainer: 100,000 books,
  100,000 editions, 300,000 author links and 200,000 category links, in a rolled-back
  transaction. It checks bounded page size, complete metadata, no page overlap,
  repeatability and a category/title search. No development data is inserted.
- Observed warm first-page handler times: 13 ms recently-added; 14 ms title.
  Includes count and metadata queries, excludes setup, network delivery and images.
  These are one local run, not a before/after comparison or concurrency SLA.
- One real cover (12586627): L = 50,835 bytes; M = 18,569 bytes, 63.5% fewer bytes.
  This is a single sample, not an average across all covers or a page-speed claim.
- Browser verified 12 medium URLs, two eager/high and ten lazy images; all loaded
  and the existing 124x184 desktop layout remained intact.

## Remaining limits

Open Library latency is outside the app's control. Its documented S/M/L variants
and direct image URLs are used; no mass downloader or arbitrary URL proxy is added.
Source: https://openlibrary.org/dev/docs/api/covers

Deep OFFSET pages, broad substring searches/counts and high concurrent traffic
need separate measurements. For that scale consider cursor pagination, trigram
search indexes and bounded server caching based on actual query plans. The current
test establishes correctness at 100k and a warm first-page measurement only.
Recommendation endpoints are separate from this catalog optimization.

Run: `npm run test:catalog` in src/frontend; run API tests filtered to
`BrowseBooksHandlerTests|CatalogScaleTests` with Docker available.
