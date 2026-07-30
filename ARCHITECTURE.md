# KitapKivo Architecture

## Status

Accepted on 2026-07-30. This document describes the initial target
architecture. Decisions may evolve through Architecture Decision Records
(ADRs), but module boundaries must not be changed implicitly.

## Architectural Style

KitapKivo is a modular monolith:

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
| `Catalog` | Books, authors, genres, identifiers, normalized metadata, and Google Books integration |
| `Reading` | Personal library entries and reading state |
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

## Dependency Direction

Within a module, dependencies point toward the domain:

```text
Endpoint -> Feature/Application -> Domain
Infrastructure -> Feature/Application and Domain abstractions
```

The domain does not depend on ASP.NET Core, Entity Framework Core, HTTP,
PostgreSQL, JWT, JSON serialization, or Google Books.

The API project is the composition root. It configures the host, registers
modules, and maps module endpoints, but it does not contain business rules.

## Vertical Slices

Features are grouped by use case rather than by technical type:

```text
KitapKivo.Modules.Catalog/
├── Domain/
├── Features/
│   ├── SearchBooks/
│   └── GetBookDetails/
├── Infrastructure/
├── Contracts/
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
community
```

Each module owns its `DbContext`, entity configurations, migrations, and
tables. Entity Framework Core already provides repository and unit-of-work
behavior, so generic repositories are not introduced. A specialized
repository abstraction is allowed only when the domain model needs one.

## Google Books Integration

Google Books is the initial external source for book discovery and metadata.
It is an infrastructure dependency behind a Catalog-owned abstraction.

The expected flow is:

1. A user searches through a KitapKivo API endpoint.
2. `Catalog` queries Google Books and normalizes the response.
3. Search results may remain transient.
4. When a user adds, rates, reviews, or otherwise interacts with a book,
   `Catalog` creates or reuses a local book record.
5. The local record stores an internal `BookId`, the Google Books volume ID,
   and the normalized metadata snapshot needed by KitapKivo.
6. `Reading` and `Community` reference the internal `BookId`, never the
   external provider identifier.

This preserves user-generated data if Google Books is unavailable or changes
its metadata. Provider credentials and secrets are supplied through
configuration and are never committed to the repository.

## API Conventions

- ASP.NET Core Minimal APIs grouped under `/api/v1`
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
KitapKivo/
├── src/
│   ├── backend/
│   │   ├── Api/KitapKivo.Api/
│   │   ├── BuildingBlocks/KitapKivo.SharedKernel/
│   │   └── Modules/
│   │       ├── Identity/KitapKivo.Modules.Identity/
│   │       ├── Catalog/KitapKivo.Modules.Catalog/
│   │       ├── Reading/KitapKivo.Modules.Reading/
│   │       └── Community/KitapKivo.Modules.Community/
│   └── frontend/kitapkivo-web/
├── tests/
│   ├── Architecture/KitapKivo.ArchitectureTests/
│   ├── Integration/KitapKivo.IntegrationTests/
│   └── Modules/
├── docs/decisions/
├── ARCHITECTURE.md
├── PROJECT_CONTEXT.md
├── ROADMAP.md
├── Directory.Build.props
├── global.json
└── KitapKivo.slnx
```

