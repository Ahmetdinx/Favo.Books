# ADR 0007: Source-backed novel eligibility

- Date: 2026-09-17
- Status: Accepted; catalog and recommendation visibility enforced

Update: new-book write/import enforcement is now implemented; see
[novel import policy](../novel-import-policy.md). Catalog browsing, recommendation
candidates and genre counts now enforce eligibility. Historical book details and
library summaries remain readable to preserve existing library access. Unknown
records are retained; only an explicit latest NotNovel review permits the novel
cleanup command to archive and delete. The staged-rollout notes below describe
the earlier foundation checkpoint. The requested 1,000-book import remains pending.

## Decision

Provider subject-label triage is not publication approval. Persist explicit
editorial Novel / NotNovel / Unknown decisions separately in an append-only
application workflow (`catalog.NovelReviews`). Each decision keeps source URL,
reason, reviewer, policy version, batch ID and recording time. A monotonic database
ID defines latest decision; missing review is Unknown. Correction is another
decision, not deletion of previous evidence.

The central eligible-novel query requires the latest decision to be Novel AND
a current covered Turkish edition. Cover presence is metadata, not visual proof.
Classification does not confer popularity or recommendation quality.

The offline batch handler validates exact work/title/author identity and requires
covered Turkish editions for approvals, with atomic application and idempotent
batch IDs. It is not a public API. Existing records and user history survive.
No cross-module database access or global query filter is introduced.

## Initial verified seed

Five existing records were source-reviewed and applied locally in batch
`classic-novels-20260917-v1` (see data/curation/classic-novels-v1.json):

- Moby Dick — [Can](https://www.canyayinlari.com/moby-dick-9789750741388).
- Anna Karenina — [İş Kültür](https://www.iskultur.com.tr/anna-karenina-2.aspx).
- Karamazov Kardeşler — [İş Kültür](https://www.iskultur.com.tr/karamazov-kardesler-2.aspx).
- Babalar ve Oğullar — [İletişim](https://iletisim.com.tr/kitap/babalar-ve-ogullar/9037).
- Bin Dokuz Yüz Seksen Dört — [Can publisher preview](https://www.canyayinlari.com/Uploads/Docs/117991/9789750718533.80_preview.pdf).

Evidence is work-level; differing editions are explicitly noted. The local
Suç ve ceza record /works/OL32284751W has author Nusret Yılmaz, so it was not
mistakenly approved as Dostoyevsky's novel. No conclusion about its actual form
was recorded merely from this mismatch. Don Kişot was deferred because cached
edition metadata needs reconciliation. Turkish classics remain to be curated.

## Staged rollout and verification

Migration 20260917123207_AddNovelReviews only creates the review table and indexes;
it contains no local GUIDs or book deletions. Applied locally; five decisions saved.
Preview found all five exact matches before application.

90 Catalog unit tests and three PostgreSQL integration tests passed. Integration
coverage verifies preview has no writes, repeated application is idempotent,
changed batches cannot overwrite evidence, corrections retain history, wrong
authors abort all entries, and a book loses eligibility when its Turkish edition
is removed/changed. New migrations run in the isolated test database.

Public browse/details/categories/summaries/recommendations and write paths are
NOT gated yet. Enabling only one would leave bypasses and inconsistencies; gating
everything now would expose just five novels. Next: expand reviewed coverage,
preserve archived private library access, then apply the shared policy across
all read/write paths and make unsupported/unreviewed outcomes explicit.
