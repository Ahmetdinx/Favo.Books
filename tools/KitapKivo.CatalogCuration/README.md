# Editorial novel decisions

This offline tool records reviewed literary form. It never fetches sources,
creates books, changes titles/authors/editions or deletes library history.
Apply Catalog migrations before use. API user secrets or the usual database
environment configuration are used; credentials are not written to output.

Preview validates every work reference, exact local title, exact author and
covered Turkish edition for Novel decisions, without saving:

```powershell
dotnet run --project tools/KitapKivo.CatalogCuration -c Release -- data/curation/classic-novels-v1.json
```

Add `--apply` to append the reviewed batch transactionally. A mismatch anywhere
rejects the whole batch. Repeat application of identical content is idempotent;
changed content must use a new batch ID. Corrections append decisions rather than
rewriting history. An advisory transaction lock serializes cooperating batches.
Latest database-generated review ID determines the active decision.

Only trusted editorial batch files belong here. The tool does NOT verify the
content of evidence URLs automatically or certify cover artwork. No HTTP endpoint
exposes this operation; an authenticated user cannot self-approve catalog books.
Reviewer `codex-assisted-source-review` denotes source review by this development
assistant, not approval by a human librarian. Sources were consulted 2026-09-17.

`catalog.NovelReviews` stores Novel / NotNovel / Unknown, source URL, evidence
note, reviewer, policy version and recording timestamp. No review also means
Unknown. Book foreign keys restrict deletion to protect review provenance.
Old physical-cleanup tools therefore require a separate policy before deleting
reviewed books. Reverting the migration drops review history and is not a normal
way to correct a decision: append Unknown or NotNovel with evidence instead.

`NovelEligibility.EligibleNovels()` combines latest explicit Novel approval with
a current nonblank Turkish-edition cover URL. It is SQL-translatable and tested
against PostgreSQL. It is deliberately NOT yet connected to public routes:
five curated works are a verification seed, not sufficient launch coverage.
Existing catalog, library and recommendations retain their behavior until the
full transition and private-history behavior are implemented together.

Seed evidence is work-level unless its note explicitly confirms a matching
edition ISBN. It does not claim measured popularity or prove a particular cover
belongs to the cited publisher's edition. Different publishers may publish the
same work. Exact local title/author guards intentionally require reconciling
transliterations with the batch file rather than accepting fuzzy matches.
