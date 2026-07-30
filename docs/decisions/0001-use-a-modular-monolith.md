# ADR 0001: Use a Modular Monolith

- Status: Accepted
- Date: 2026-07-30

## Context

KitapKivo needs clear boundaries for identity, catalog, personal reading, and
community features. The product is at an early stage and does not yet have the
scale, team topology, or independent deployment requirements that justify
distributed services.

A traditional layered solution would be simple initially, but unrelated
features would accumulate inside shared Application and Infrastructure
projects. Microservices would introduce network, deployment, observability,
messaging, and distributed-data costs before those costs provide value.

## Decision

Build KitapKivo as one deployable ASP.NET Core application composed of
separate business modules:

- Identity
- Catalog
- Reading
- Community

Use vertical slices inside each module, Clean Architecture dependency rules,
tactical Domain-Driven Design where useful, and lightweight CQRS.

Each module owns its persistence schema and exposes only explicit contracts.
Architecture tests will enforce the most important boundaries.

## Consequences

### Positive

- Local development and deployment remain simple.
- Business capabilities remain discoverable and independently maintainable.
- Transactions and operational diagnostics stay straightforward.
- A module can be extracted later if measured needs justify it.

### Negative

- Boundaries require discipline because all modules run in one process.
- In-process calls can tempt developers to bypass contracts.
- A single deployment unit cannot scale modules independently.

## Revisit When

Reconsider service extraction only when a module has proven independent
scaling, deployment cadence, reliability, security, or team-ownership needs.

