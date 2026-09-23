# Verified publisher novel import

Offline operator workflow; not a public API or automatically scheduled worker.
The Open Library allowlist and existing guarded import endpoints remain unchanged.

1. Collect publisher product facts with `tools/collect_publisher_novels.py` and,
   optionally, `tools/collect_ithaki_novels.py`. Requires `beautifulsoup4` and
   `Pillow`; local dependencies and raw pages belong under ignored `artifacts/`.
2. Review the collector's rejections and source samples. A list category alone is
   insufficient: each product must repeat a novel classification, identify its
   author and ISBN, and supply its edition's cover. Exclude contradictory forms,
   sets, ambiguous authorship and invalid ISBNs. No descriptions are imported.
   `data/curation/publisher-form-reviews-v1.json` records source-hash-bound manual
   decisions. `Hold` excludes a record; `Novel` can resolve a literary-form keyword
   false positive, but cannot bypass identity, publisher or cover validation.
   Reviewed Doğan Novus/SoLibri novels retain their actual imprint rather than
   being mislabeled Doğan Kitap. Other publisher conflicts remain held.
3. After collectors finish, run `tools/prepare_publisher_manifest.py <directory>`
   to merge their facts and the optional `collect_publisher_genres.py` output.
   Then run `tools/verify_publisher_covers.py <directory> --input merged-publisher-candidates.json`.
   This checks cached source hashes,
   contradictory forms, image response headers and dimensions. Empty Doğan
   descriptions are held for review because its broad Romanlar list also contains
   poetry. Cover checks older than 24 hours are refreshed, including failures.
   It does not claim
   a human visual inspection of every cover. It produces
   `verified-publisher-novels.json` and `verification-summary.json`.
4. Preview using the API's local user secrets or environment configuration:

```powershell
dotnet run --project tools/KitapKivo.PublisherImport -c Release -- <manifest.json> 1000 <stable-batch-id> <preview.json>
```

5. Inspect the selected titles and duplicate count. Append `--apply` to import
   exactly the requested number in one transaction. The command refuses to write
   if there are insufficient new titles or no matching, recent verification
   report. Use a separate result path for apply. Keep manifests and result hashes.

Deduplication checks all existing books and editions, including hidden records,
by ISBN-13 (including conversion from ISBN-10), publisher URL and normalized
title. Title matching deliberately skips ambiguous homonymous works rather than
inventing identity links. It strips parenthetical edition details and common
binding and pocket-edition suffixes, plus reviewed alternate titles and 1Q84's
separate volumes. This is conservative deduplication, not a complete authority
file for translated alternate titles. A publisher edition of an existing book
never counts toward the new-book target.

Only new books, editions, associated author/publisher/category rows, source
references and Novel reviews are written. Existing books and user records are
not changed. The tool shares the seed advisory lock. A stable batch ID makes a
completed run repeatable without adding a second set of books; a changed batch
or mismatched manifest is rejected. Failures before commit roll back all writes.

Original publication dates are not inferred from edition dates. Unknown original
languages are stored as `und`. Doğan's hidden original-language field is not used:
it also reports Turkish on translated works. Unknown physical formats stay `Unknown`. Genre
tags are assigned only from known source categories; a broad subject is not
automatically a novel subgenre. Priority favors publisher classical series and
does not claim measured readership or sales rankings.

After import, check the applied result's book IDs, total and eligible counts,
cover availability, and the running catalog API. Database data is local; pushing
code alone does not upload the imported records to another environment.
