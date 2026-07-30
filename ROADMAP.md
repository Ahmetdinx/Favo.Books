# KitapKivo Roadmap

This roadmap is ordered by dependency and learning value. Each item should be
delivered through a small, reviewable commit. Scope may change through an ADR
or an explicit product decision.

## Completed

- [x] Initialize the solution and Git repository.
- [x] Define the initial product scope and architecture.
- [x] Select Google Books as the initial discovery and metadata source.
- [x] Define the MVP social scope: library entries, ratings, reviews, and
      review comments.

## Phase 1 - Backend Foundation

- [ ] Pin the .NET SDK with `global.json`.
- [ ] Add shared build rules with `Directory.Build.props`.
- [ ] Create the API, Shared Kernel, and initial module projects.
- [ ] Configure project references and module registration.
- [ ] Add architecture tests for dependency boundaries.
- [ ] Add API exception handling, `ProblemDetails`, OpenAPI, and a health
      endpoint.

## Phase 2 - Identity and Persistence

- [ ] Configure PostgreSQL and local development settings.
- [ ] Establish module-owned schemas and migrations.
- [ ] Implement registration and login.
- [ ] Add authorization policies and refresh-token handling.
- [ ] Add identity integration tests.

## Phase 3 - Catalog

- [ ] Define the Google Books client abstraction and configuration.
- [ ] Implement book search and normalized result contracts.
- [ ] Persist or reuse a local book record on the first user interaction.
- [ ] Implement book details.
- [ ] Add resilience, validation, and integration tests.

## Phase 4 - Personal Library

- [ ] Add a book to the user's library.
- [ ] List the user's library.
- [ ] Remove a book from the user's library.
- [ ] Add reading states only when their user experience is defined.

## Phase 5 - Community

- [ ] Create or update a book rating.
- [ ] Publish, edit, and delete a review.
- [ ] Add, edit, and delete comments on reviews.
- [ ] Display aggregate ratings and paginated reviews.
- [ ] Add ownership, authorization, and moderation-ready rules.

## Phase 6 - Web Application

- [ ] Establish the Next.js frontend.
- [ ] Implement authentication flows.
- [ ] Implement search and book details.
- [ ] Implement the personal library.
- [ ] Implement ratings, reviews, and comments.
- [ ] Add end-to-end tests for critical journeys.

## Later Candidates

- User-to-user following
- Likes and activity feeds
- Notifications
- Dedicated administration and moderation interfaces
- Recommendations
- Dedicated search infrastructure
- Caching
- Service extraction when measured operational needs justify it

