# KitapKivo Project Context

## Current State

KitapKivo has a clean .NET 10 solution and a Git repository connected to
GitHub. The initial product scope and architectural direction are documented.
No backend or frontend project has been created yet.

## Product Decisions

- Google Books is the initial source for book discovery and metadata.
- Search results can be transient.
- A local Catalog record is created or reused when a user first interacts
  with a book.
- User-generated data references KitapKivo's internal `BookId`.
- The MVP includes personal library entries, ratings, reviews, and comments
  on reviews.
- "Follow" does not mean adding a book to a library. User-to-user following
  is postponed.
- A dedicated administration panel is not part of the initial MVP.

## Architecture Decisions

- Modular monolith
- Vertical slices inside business modules
- Clean Architecture dependency direction
- Tactical Domain-Driven Design where business rules need it
- Lightweight CQRS without event sourcing or separate databases
- Minimal APIs
- PostgreSQL with a schema and `DbContext` per module
- Architecture tests to protect boundaries

## Initial Modules

- `Identity`
- `Catalog`
- `Reading`
- `Community`

## Completed Work

1. `chore: initialize solution and repository`
2. `docs: define initial architecture and roadmap`

## Next Commit

```text
chore: establish backend solution structure
```

The next commit should create only the backend project structure, shared build
configuration, solution folders, and project references. It should not
implement product features or add database persistence.

## Working Agreement

For each commit:

1. Confirm the repository and working-tree state.
2. Define one coherent scope.
3. Explain the purpose and architectural impact.
4. Implement the smallest complete change.
5. Run relevant validation.
6. Review the staged diff.
7. Commit with a Conventional Commit message.
8. Push and record the next step.

