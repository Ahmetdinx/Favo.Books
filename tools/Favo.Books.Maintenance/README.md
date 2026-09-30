# Catalog cleanup

Novel cleanup uses `report --confirmed-non-novels` and then
`apply --confirmed-non-novels`. It selects only books whose latest editorial
review is `NotNovel`. Missing reviews, `Unknown` decisions and subject-classifier
candidates never authorize deletion. Unknown books are excluded from catalog and
recommendation queries but remain stored with their user records.

Backups include `catalog_NovelReviews` and `community_RecommendationFeedback`.
Reviews are explicitly removed after backup because their book foreign key is
restrictive. Restore reviews after books, preserving review IDs; advance the
identity sequence if restoration introduces a higher ID. Restore feedback with
the other user records. Report mode was checked on 2026-09-17: 9,858 books,
zero confirmed non-novels eligible for deletion; no records were deleted.

Explicit, one-time maintenance outside module request handlers. Run with the API's
user secrets or `ConnectionStrings__Database` environment variable; credentials
are never printed. Stop API/import workers before applying maintenance.

```powershell
dotnet run --project tools/Favo.Books.Maintenance -- report
dotnet run --project tools/Favo.Books.Maintenance -- apply
dotnet run --project tools/Favo.Books.Maintenance -- report
```

A book is selected when none of its editions has BOTH language `tr`/`tur`
(case/whitespace normalized) AND a nonblank cover URL. A foreign cover alone does
not save a book. Remote URL availability is not checked. This does not change the
import policy: new imports require reviewed novels and covered Turkish editions.

`report` performs no persistent writes. `apply` locks affected tables, selects
candidates, copies all affected rows to a timestamped `catalog_cleanup_*` schema,
removes library/profile/rating references and deletes books in one transaction.
Catalog editions, author/category links and book/edition external references
cascade. Shared authors, categories and publishers remain. Failure rolls back
both archive creation and deletion. A repeated run has no effect if no new
candidates exist. This is not an application startup task or recurring purge.

On 2026-09-16, local database `kitapkivo`: 154 books, 157 editions, 166 author links,
512 category links, 310 external references, four library entries, one bedside
selection and one influential selection were archived and removed. No ratings
were affected. 103 books remained; the next report found zero candidates.
Recovery schema: `catalog_cleanup_20260916074514901`.

For recovery, stop writers and restore in a transaction, using explicit columns
from the archive tables into their original tables. Restore catalog_Books first,
then catalog_Editions, catalog_BookAuthors, catalog_BookCategories and
catalog_ExternalReferences; restore reading_LibraryEntries, profiles_BedsideBooks,
profiles_InfluentialBooks and community_BookRatings last. Check for conflicting
book imports or user selections first; do not overwrite newer user data. Archive
tables are data copies without constraints, kept in the same database and covered
by its backups. No automatic archive deletion is implemented.
