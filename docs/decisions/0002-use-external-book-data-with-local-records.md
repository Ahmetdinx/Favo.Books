# ADR 0002: Use External Book Data with Local Catalog Records

- Status: Accepted
- Date: 2026-07-30
- Updated: 2026-08-06

## Context

KitapKivo needs a broad book catalog without manually entering all book
metadata. Ratings, reviews, comments, and personal library entries must remain
stable even if an external provider is slow, unavailable, removes an item, or
changes its data.

No single free external provider offers complete and consistently reliable
coverage for every book, edition, author, and language relevant to KitapKivo.
The Catalog module therefore needs a provider-independent integration boundary.

## Decision

Use Open Library as the initial and primary external source for book discovery
and metadata.

Use Wikidata where appropriate to enrich author identities, biographical
information, awards, and relationships. Google Books may be used as a secondary
fallback and comparison source when Open Library data is missing or uncertain.

All external integrations belong to the Catalog module behind Catalog-owned
abstractions. Provider-specific response types must not leak into the domain,
other modules, or public API contracts.

Search results may remain transient. On the first meaningful user interaction
with an external book, Catalog creates or reuses a local record containing:

- An internal `BookId`
- Relevant external provider identifiers
- A normalized metadata snapshot required by KitapKivo

Reading and Community records reference the internal `BookId`, never an
external provider identifier.

Current Turkish editions may later be obtained through agreements with Turkish
publishers or distributors. Millî Kütüphane pages will not be scraped.
Commercial ISBN providers will only be considered after measuring coverage gaps
in the free data sources.

## Consequences

### Positive

- KitapKivo can offer useful discovery without a manually curated catalog.
- User-generated content remains stable during provider outages or changes.
- The system is not permanently coupled to a single external provider.
- Provider-specific response types remain inside infrastructure boundaries.
- Additional providers can be introduced without changing Reading or Community.

### Negative

- Combining multiple providers increases mapping and deduplication complexity.
- Local metadata can become stale.
- Provider identifiers and metadata quality may differ between sources.
- Refresh, merge, conflict-resolution, and fallback policies must be defined as
  the catalog grows.
- Availability, rate limits, and terms remain external constraints.

## Revisit When

Define explicit refresh, merge, deduplication, and provider-priority policies
after observing real catalog quality, coverage gaps, and usage.