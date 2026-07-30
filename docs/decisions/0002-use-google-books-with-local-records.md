# ADR 0002: Use Google Books with Local Catalog Records

- Status: Accepted
- Date: 2026-07-30

## Context

KitapKivo needs a broad book catalog without manually entering all book
metadata. Ratings, reviews, comments, and personal library entries must remain
stable even if an external provider is slow, unavailable, removes an item, or
changes its data.

## Decision

Use Google Books as the initial external source for discovery and metadata.
The integration belongs to the Catalog module behind an application-owned
abstraction.

Search results may remain transient. On the first user interaction with a
book, Catalog creates or reuses a local record containing:

- An internal `BookId`
- The Google Books volume ID
- A normalized metadata snapshot required by KitapKivo

Reading and Community records reference the internal `BookId`, not the Google
Books identifier.

## Consequences

### Positive

- KitapKivo can offer useful discovery without a manually curated catalog.
- User-generated content remains stable during provider outages or changes.
- Provider-specific response types do not leak into other modules.
- A second metadata provider can be added later behind the same boundary.

### Negative

- Local metadata can become stale.
- Deduplication and refresh policies must be defined as the catalog grows.
- Google Books quotas, availability, and terms remain external constraints.

## Revisit When

Define explicit refresh, merge, and alternative-provider policies after
observing real catalog quality and usage.

