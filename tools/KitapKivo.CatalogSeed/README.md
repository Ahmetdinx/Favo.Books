# Covered Turkish-edition catalog seed

Explicit local maintenance command; no automatic imports on application startup.
Uses the API's user secrets or `ConnectionStrings__Database`. Run from the repo root:

```powershell
dotnet run --project tools/KitapKivo.CatalogSeed -- 1000 artifacts/catalog-import-20260916
```

The number is the desired count of newly created books for this run directory.
Open Library Search is queried with `language:tur`, `lang=tr` and explicit nested
edition fields. Only returned editions with a Turkish language tag, positive
edition cover ID, title and work authors are accepted. Work-level covers are never
used as edition covers. See https://openlibrary.org/dev/docs/api/search.

Source search pages are cached and successful book IDs appended to `created.jsonl`.
Keep the directory for provenance and targeted recovery. Reusing it resumes the
run; never delete its manifest while trying to resume. After an interrupted process,
reconcile committed book IDs with the manifest before rerunning (a commit immediately
before a manifest write can otherwise be missing from the manifest).

Existing Open Library work/edition references, ISBNs and matching title/author pairs
are skipped. The existing ResolveExternalBookHandler creates authors, publishers,
subject-category mappings, editions and references in its normal EF transaction.
Read-only adapters provide real cached search records to the handler, avoiding
thousands of redundant work/author/edition requests. Original language is `und`
when the source search record does not establish it; Turkish edition language is
stored separately. Unavailable page count/format/description stays unset.

Requests are sequential, with a minimum one-second delay and bounded retries.
Each book is committed separately. Constraint/validation failures are logged and
skipped; an unexpected failure stops the process. An advisory lock prevents two
instances of this tool running concurrently. This is not a general concurrent
import scheduler. Run while the normal import worker is disabled.

A positive cover ID means the provider lists a cover; this command does not
download or visually validate every image. It writes book metadata only.

2026-09-16 completed run: 1,000 created, 319 skipped, zero persistence failures,
14 source pages. Local catalog increased from 103 to 1,103. Verification found all
1,000 IDs present, no new books missing a covered Turkish edition or author,
no new foreign-language editions and no duplicate ISBN-13 involving new books.
