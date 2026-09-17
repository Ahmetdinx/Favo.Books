# Read-only novel evidence audit

Run from the repository root with the API's existing user secrets or
`ConnectionStrings__Database` environment configuration:

```powershell
dotnet run --project tools/KitapKivo.CatalogAudit -c Release -- artifacts/catalog-audit-new artifacts/catalog-import-20260916 artifacts/catalog-import-20260917
```

The output directory must not exist. Input directories contain the previously
cached Open Library `search-*.json` responses. No provider requests or imports
are performed. The database transaction is repeatable-read and explicitly
READ ONLY; only catalog books, external work references and edition metadata
are read. No user data or credentials are written to the report.

Outputs (ignored under artifacts):

- `books.jsonl`: one row per database book, work references, covered Turkish
  edition flag, source paths/subjects, matched evidence and versioned result.
- `summary.json`: counts, timestamp and ten deterministic samples per group.
- `sources.json`: source file paths and SHA-256 hashes for reproducibility.

`NovelEvidenceClassifier` lives in Catalog and has unit coverage. Only exact
normalized literary-form labels are considered. General `fiction`, `classics`,
`history`, `finance` and title words are not proof. All cached observations for
a work are retained; conflicting signals remain conflicts rather than letting
the newest file silently override older evidence. Categories in the database
are not used because their existing broad mapping loses source specificity.

Results are **candidates for review**, never publication eligibility or deletion
instructions. Even explicit provider labels can be incorrect or refer to a
book's subject rather than its own form. Missing source files/subjects produce
Unknown. Coverage counts mean a cached record was found, not that it contains
useful evidence. Cover checks only establish a stored nonblank URL, not an image
download or correct-edition visual validation.

The intentionally narrow v1 vocabulary also sends unfamiliar form labels to
Unknown. It does not establish canonical literary classifications. Short-form
and graphic works require editorial scope decisions rather than promotion by
substring matching. Review evidence before enabling any catalog filter.

The first local run and next steps are recorded in
`docs/catalog-novel-audit.md`. This tool does not alter existing catalog behavior.
