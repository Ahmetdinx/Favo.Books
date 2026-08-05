# KitapKivo Roadmap

This roadmap is ordered by dependency and learning value. Each item should be
delivered through a small, reviewable commit. Scope may change through an ADR
or an explicit product decision.

## Product and Data Decisions

- [x] Define the initial product scope and architecture.
- [x] Define the MVP social scope: personal libraries, ratings, reviews, and
      comments on reviews.
- [x] Keep in-app book reading outside the product scope.
- [x] Keep user-to-user following outside the initial MVP.
- [x] Select Open Library as the primary book discovery and metadata source.
- [x] Select Wikidata for author and relationship enrichment.
- [x] Keep Google Books as a secondary fallback and comparison source.
- [x] Keep commercial ISBN providers outside the MVP until data gaps are
      measured.
- [x] Avoid scraping Millî Kütüphane pages.

## Phase 1 - Backend Foundation

- [x] Pin the .NET SDK with `global.json`.
- [x] Add shared build rules with `Directory.Build.props`.
- [x] Create the API, Shared Kernel, and initial module projects.
- [x] Configure project references and module registration.
- [x] Add architecture tests for dependency boundaries.
- [x] Add Swagger/OpenAPI and a health endpoint.
- [ ] Add centralized exception handling and `ProblemDetails`.

## Phase 2 - Identity and Persistence

- [x] Configure PostgreSQL and local development settings.
- [x] Establish the Identity-owned schema and migrations.
- [x] Implement the initial user domain model.
- [x] Add user domain tests.
- [x] Implement user registration.
- [x] Implement JWT login and authentication.
- [x] Add authenticated profile access.
- [x] Add PostgreSQL-backed API integration test infrastructure.
- [x] Add authentication endpoint integration tests.
- [ ] Add refresh-token handling if required by the final session strategy.

## Phase 3 - Catalog Foundation

- [x] Define the `Author`, `Book`, and `BookAuthor` domain models.
- [x] Add Catalog-owned PostgreSQL persistence and migrations.
- [x] Implement author creation.
- [x] Implement book creation with ordered authors.
- [x] Retrieve a local book through KitapKivo's internal `BookId`.
- [ ] Add Catalog domain and integration test coverage.

## Phase 4 - External Book Discovery

- [ ] Define the Open Library client abstraction, options, and external
      contracts.
- [ ] Implement the Open Library HTTP client.
- [ ] Map Open Library data into normalized Catalog search results.
- [ ] Expose the public book-search endpoint.
- [ ] Add search validation, resilience, and tests.
- [ ] Enrich author information through Wikidata where appropriate.
- [ ] Retrieve external book details.
- [ ] Create or reuse a local Catalog record when the user first interacts
      with an external book.
- [ ] Use Google Books as a secondary fallback for missing or uncertain
      records.
- [ ] Measure coverage gaps before evaluating additional data providers.

## Phase 5 - Personal Library

- [ ] Add a book to the user's personal library.
- [ ] List the user's personal library.
- [ ] Remove a book from the user's personal library.
- [ ] Define and implement reading states.
- [ ] Add ownership and authorization rules.
- [ ] Add domain and integration tests.

## Phase 6 - Community

- [ ] Create or update a book rating.
- [ ] Publish, edit, and delete a review.
- [ ] Add, edit, and delete comments on reviews.
- [ ] Display aggregate ratings and paginated reviews.
- [ ] Add ownership, authorization, and moderation-ready rules.
- [ ] Add domain and integration tests.

## Phase 7 - Web Application

- [x] Establish the React and Vite frontend.
- [x] Implement registration and login forms.
- [x] Connect authentication forms to the Identity API.
- [x] Add authentication session management and protected routes.
- [x] Complete the authenticated profile flow.
- [ ] Implement external book search.
- [ ] Implement book details.
- [ ] Implement the personal library.
- [ ] Implement ratings, reviews, and comments.
- [ ] Establish the visual Kivo identity and responsive design system.
- [ ] Add end-to-end tests for critical user journeys.

## Phase 8 - Production Readiness

- [ ] Add structured logging and production observability.
- [ ] Define production configuration and secret management.
- [ ] Establish the production database migration strategy.
- [ ] Add continuous integration and deployment workflows.
- [ ] Add rate limiting and external API protection.
- [ ] Complete security, performance, accessibility, and reliability checks.
- [ ] Deploy the first public MVP.

## Future Data Sources

- Agreements with Turkish publishers or distributors for current Turkish
  editions
- Millî Kütüphane verification or institutional collaboration
- Commercial ISBN providers if measured coverage gaps justify their cost

## Later Product Candidates

- User-to-user following
- Likes and activity feeds
- Notifications
- Dedicated administration and moderation interfaces
- Personalized recommendations
- Dedicated search infrastructure
- Caching
- Service extraction when measured operational needs justify it

## Current Milestone

Integrate Open Library as KitapKivo's primary external book discovery source.

The next planned implementation commit is:

```text
feat(catalog): add Open Library client foundation