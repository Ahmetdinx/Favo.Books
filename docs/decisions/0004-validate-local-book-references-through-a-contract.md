# Validate local book references through a contract

Accepted: 2026-09-16.

## Context

Reading and Profiles previously accepted any non-empty BookId. API callers
could create library entries or profile selections for books absent from Catalog.
Modules must not access another module's DbContext or tables.

## Decision

Expose the minimal read-only IBookCatalog.ExistsAsync contract in SharedKernel,
which both consumers already reference. Catalog supplies the scoped implementation
and queries its own persistence. No entities, EF types, or provider identifiers
are exposed by the contract. Its placement is a deliberate, narrow addition for
two existing consumers, not a general shared service layer.

The add-library, set-bedside, and set-influential handlers validate the local
BookId before accessing their own persistence. Missing books produce HTTP 404
ProblemDetails. Rejected replacements preserve the existing selection.
Empty identifiers still produce HTTP 400; duplicate selections still produce 409.

## Consequences

Each of these writes adds one indexed Catalog existence query. Cancellation and
Catalog failures propagate; a Catalog failure must not be treated as a missing book.
Project reference boundaries remain unchanged. No database migration is needed.

This prevents new dangling references through these operations; it does not repair
existing data or provide a cross-module foreign key. Catalog currently exposes no
book deletion endpoint. If deletion is introduced, its concurrent-write and
reference-retention policy must be designed explicitly.
