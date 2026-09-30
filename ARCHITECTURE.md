# Favo.Books Architecture

## Status

Accepted on 2026-07-30; updated for current implementation on 2026-09-16.
Decisions may evolve through Architecture Decision Records
(ADRs), but module boundaries must not be changed implicitly.

## Architectural Style

Favo.Books is a modular monolith:

- One backend application is deployed as a single unit.
- Business capabilities are separated into explicit modules.
- Features are organized as vertical slices inside each module.
- Clean Architecture rules keep domain logic independent from frameworks.
- Tactical Domain-Driven Design is used where business rules justify it.
- Commands and queries are separated with lightweight CQRS.

The project does not initially use microservices, event sourcing, a message
broker, separate read and write databases, or a generic repository layer.

## Initial Modules

| Module | Responsibility |
| --- | --- |
| `Identity` | Registration, authentication, authorization, account security, and basic profiles |
| `Catalog` | Books, authors, genres, identifiers, normalized metadata, and external book-data integrations |
| `Reading` | Personal library entries and reading state |
| `Profiles` | Bedside book, influential books, and personal notes |
| `Community` | Ratings, reviews, and comments on reviews |

The term "follow" is reserved for a future user-to-user relationship.
Adding a book to a personal collection is modeled as a `LibraryEntry` in the
`Reading` module.

Notifications, moderation, search infrastructure, recommendations, likes,
activity feeds, and user following will be introduced only when a concrete
use case requires them.

## Module Boundaries

- Each business module is a separate .NET class library.
- A module exposes only deliberate contracts and registration methods.
- Internal domain and persistence types are not shared across modules.
- Modules reference other concepts by stable identifiers, not by Entity
  Framework navigation properties.
- A module never accesses another module's `DbContext`, schema, or tables.
- Cross-module communication starts with explicit in-process contracts or
  events. A broker is not required inside the initial monolith.
- `SharedKernel` contains only proven cross-cutting primitives and must not
  become a general-purpose utilities project.
- Circular project references are forbidden and will be protected by
  architecture tests.

`IBookCatalog` is a minimal read-only contract in SharedKernel, implemented by
Catalog and consumed by Reading and Profiles before book writes. It exposes only
an existence query, not persistence or domain types (see ADR 0004).
Current architecture tests enforce project references; namespace and domain-layer
dependency enforcement is not yet comprehensive. Community currently implements
ratings; reviews and review comments remain pending.

## Dependency Direction

Within a module, dependencies point toward the domain:

```text
Endpoint -> Feature/Application -> Domain
Infrastructure -> Feature/Application and Domain abstractions
```

The domain does not depend on ASP.NET Core, Entity Framework Core, HTTP,
PostgreSQL, JWT, JSON serialization, or external book-data providers.
The API project is the composition root. It configures the host, registers
modules, and maps module endpoints, but it does not contain business rules.

## Vertical Slices

Features are grouped by use case rather than by technical type:

```text
Favo.Books.Modules.Catalog/
├── Domain/
├── Application/
│   └── Books/Search/
├── Presentation/
│   └── Books/Search/
├── Infrastructure/
├── Persistence/
└── CatalogModule.cs
```

A slice may contain its request, response, validation, endpoint, command or
query, and handler. Shared code is extracted only after multiple slices prove
that the abstraction is useful.

## Persistence

The application uses one PostgreSQL database with a schema per module:

```text
identity
catalog
reading
profiles
community
```

All five schemas are implemented; Community currently contains book ratings.
Each persistent module owns its `DbContext`, entity configurations, migrations, and
tables. Entity Framework Core already provides repository and unit-of-work
behavior, so generic repositories are not introduced. A specialized
repository abstraction is allowed only when the domain model needs one.

## External Book Data Integration

Open Library is the initial and primary external source for book discovery and
metadata. Wikidata may enrich author identities and related information.
Google Books is a secondary fallback and comparison source.

All external providers are infrastructure dependencies behind Catalog-owned
abstractions. Provider-specific response types must not leak into the domain,
other modules, or public API contracts.

The expected flow is:

1. A user searches through a Favo.Books API endpoint.
2. `Catalog` queries Open Library and normalizes the response.
3. Search results may remain transient.
4. Author information may be enriched through Wikidata where appropriate.
5. When a user adds, rates, reviews, or otherwise interacts with a book,
   `Catalog` creates or reuses a local book record.
6. The local record stores an internal `BookId`, relevant external provider
   identifiers, and the normalized metadata snapshot needed by Favo.Books.
7. `Reading` and `Community` reference the internal `BookId`, never an external
   provider identifier.
8. Google Books may be queried when Open Library data is missing or uncertain.

This preserves user-generated data if an external provider is unavailable or
changes its metadata. Provider credentials and secrets, when required, are
supplied through configuration and are never committed to the repository.

## API Conventions

- ASP.NET Core Minimal APIs grouped under `/api/v1`
- Current routes use `/api/identity`, `/api/catalog`, `/api/reading`, and
  `/api/profiles`. The original `/api/v1` convention above is a target, not the
  current contract; introducing versioned routes requires an explicit decision.
- Standard `ProblemDetails` error responses
- Central exception handling
- OpenAPI documentation
- Consistent validation and pagination contracts
- Cancellation token support
- Structured logging
- Policy-based authorization

## Testing Strategy

- Unit tests protect domain rules and pure application logic.
- Integration tests cover module persistence and API behavior with
  PostgreSQL-compatible infrastructure.
- Architecture tests enforce project, namespace, and module boundaries.
- Frontend end-to-end tests will be added when user journeys exist.

## Target Repository Structure

```text
Favo.Books/
├── src/
│   ├── backend/
│   │   ├── Api/Favo.Books.Api/
│   │   ├── BuildingBlocks/Favo.Books.SharedKernel/
│   │   └── Modules/
│   │       ├── Identity/Favo.Books.Modules.Identity/
│   │       ├── Catalog/Favo.Books.Modules.Catalog/
│   │       ├── Reading/Favo.Books.Modules.Reading/
│   │       ├── Profiles/Favo.Books.Modules.Profiles/
│   │       └── Community/Favo.Books.Modules.Community/
│   └── frontend/
├── tests/
│   ├── Architecture/Favo.Books.ArchitectureTests/
│   ├── Api/Favo.Books.Api.IntegrationTests/
│   └── Modules/
├── docs/decisions/
├── ARCHITECTURE.md
├── PROJECT_CONTEXT.md
├── ROADMAP.md
├── Directory.Build.props
├── global.json
└── Favo.Books.slnx
```

