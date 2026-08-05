# KitapKivo Project Context

## Current State

KitapKivo currently consists of a modular .NET 10 backend and a React
frontend.

The backend includes API, Shared Kernel, Identity, Catalog, Reading, and
Community projects. Architecture tests protect the selected dependency
boundaries, and the API exposes health-check infrastructure.

The Identity module uses PostgreSQL and supports user registration, login,
JWT authentication, and authenticated profile access. These flows are
covered by domain and API integration tests.

The frontend includes authentication forms, session management, protected
routes, and an authenticated profile flow.

The Catalog module currently includes:

- `Author`, `Book`, and `BookAuthor` domain and persistence models
- PostgreSQL-backed EF Core persistence
- Author creation
- Book creation with ordered authors
- Book retrieval by KitapKivo's internal `BookId`

The next development milestone is external book discovery through Google
Books.

## Product Direction

KitapKivo is not an in-browser book-reading platform and will not host
copyrighted book content.

Its purpose is to provide book lovers with:

- Enjoyable book discovery
- Personal libraries
- Meaningful ratings and literary reviews
- Community-driven recommendations
- Suggestions based on previously read books and created libraries
- A visually distinctive and pleasant experience

The long-term Kivo product family may also include cinema, television, and
anime-focused applications. These products should share a recognizable Kivo
identity while remaining separate experiences.

## Product Decisions

- Open Library is the initial and primary source for book discovery and metadata.
- Wikidata will enrich author identities, biographical information, awards, and
  relationships between authors and works.
- Google Books is a secondary fallback and comparison source for records that
  cannot be found reliably through Open Library. It will not be KitapKivo's
  primary catalog source.
- External search results can remain transient.
- A local Catalog record is created or reused when a user first interacts with
  an external book.
- User-generated data references KitapKivo's internal `BookId`.
- Current Turkish editions may later be obtained through agreements with
  Turkish publishers or distributors.
- Millî Kütüphane data will not be collected through page scraping. It may be
  used for verification or through a future institutional collaboration.
- Commercial ISBN providers are outside the MVP and will be considered only
  after measuring gaps in the free data sources.

## Architecture Decisions

- Modular monolith
- Vertical slices inside business modules
- Clean Architecture dependency direction
- Tactical Domain-Driven Design where business rules need it
- Lightweight CQRS without event sourcing or separate read/write databases
- ASP.NET Core Minimal APIs
- EF Core with PostgreSQL
- A schema and `DbContext` owned by each persistent module
- Architecture tests to protect module boundaries
- React frontend communicating with the backend API
- JWT-based authentication

## Core Modules

- `Identity`
- `Catalog`
- `Reading`
- `Community`

## Completed Milestones

1. Established the .NET 10 solution and modular backend structure.
2. Added shared build rules and architecture boundary tests.
3. Added the API foundation and health endpoint.
4. Implemented the Identity domain model and PostgreSQL persistence.
5. Implemented registration, login, JWT authentication, and identity tests.
6. Established the React frontend and completed the authenticated profile
   flow.
7. Added the initial Catalog domain and PostgreSQL persistence model.
8. Implemented author creation, book creation, and book retrieval use cases.

## Next Development Milestone

Integrate Open Library as KitapKivo's first external book discovery source.

The work should be delivered incrementally:

1. Define the Open Library client abstraction, options, and external contracts.
2. Implement the HTTP client and map external data into normalized Catalog
   search results.
3. Expose a book-search endpoint.
4. Add validation, resilience, and tests.
5. Enrich author information through Wikidata where appropriate.
6. Create or reuse a local Catalog record when the user first interacts with
   an external book.
7. Use Google Books only as a secondary fallback for missing or uncertain
   records.

## Next Planned Commit

```text
feat(catalog): add Open Library client foundation