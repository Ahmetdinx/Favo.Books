# ADR 0003: Define the Initial Social Scope

- Status: Accepted
- Date: 2026-07-30

## Context

The word "follow" can describe either following another user or saving a book.
Combining these concepts would blur module boundaries and create ambiguous API
and domain terminology. The MVP also needs enough social value without
building an activity network before the core reading experience exists.

## Decision

The initial product includes:

- Adding and removing books in a personal library
- Rating a book
- Publishing, editing, and deleting a review
- Commenting on a review

Personal book collection behavior belongs to the Reading module and is named
`LibraryEntry`.

Ratings, reviews, and review comments belong to the Community module.

User-to-user following, likes, activity feeds, and notifications are postponed
until a concrete product flow requires them.

## Consequences

### Positive

- Domain language and API routes remain unambiguous.
- The MVP focuses on the core discovery-to-reading-to-review journey.
- Identity, Reading, and Community responsibilities stay separate.

### Negative

- The first release will not provide a social graph or personalized activity
  feed.
- User engagement features will require later design work.

## Revisit When

Reconsider user following together with privacy rules, feed behavior,
notification preferences, blocking, and moderation requirements.

