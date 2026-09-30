# Reviewed novel import

The former broad Turkish-language search importer has been replaced. Only works
in `data/curation/novel-import-priorities-v1.json` can be newly imported. The list
is embedded in Catalog at build time, ordered by editorial priority, and shared
with the external resolve API and background import safety checks. It is not a
live most-read ranking. Currently it contains five source-reviewed classics.

Preview (no database/network access or files written):

```powershell
dotnet run --project tools/Favo.Books.CatalogSeed -c Release -- 100 artifacts/reviewed-import-new
```

Explicit execution:

```powershell
dotnet run --project tools/Favo.Books.CatalogSeed -c Release -- 100 artifacts/reviewed-import-new --apply
```

The count (1–10,000) is a maximum NEW-book count, not a promise or a random-search
quota. The tool stops when the reviewed list ends. Use a new output directory;
old broad-import directories cannot be resumed. Existing reviewed books are reused.
Unreviewed or revoked existing books are rejected, not silently approved.

New imports require approved work identity, matching source title/author and a
matching Turkish-edition title with a cover URL. A NovelReview is saved together
with the book. Unexpected metadata is rejected for review, not fuzzy-matched.
Free-form POST /api/catalog/books cannot bypass this policy (422).

API user secrets or ConnectionStrings__Database supply database configuration.
Requests use Catalog's provider request gate and retry policy; a session advisory
lock prevents concurrent seed commands. Run with the application import worker
disabled. This command builds a service provider, not a background worker host.

`policy.json` preserves the exact reviewed list; `results.jsonl` records created,
existing and rejected results. A crash can occur after DB commit but before log
append: reconcile database external references before using logs as an inventory.
A subsequent run in a new directory rechecks existing records. Rejections produce
exit code 2; upstream failures abort rather than silently changing the source.

Cover presence is source metadata, not a downloaded/visually inspected image.
The verified list must be deliberately expanded with source evidence. No fallback
to generic fiction labels, unreviewed search results or user-provided approvals.
Historical imports under artifacts remain untouched. No new import was executed
as part of this implementation; the preview is safe to run for verification.
