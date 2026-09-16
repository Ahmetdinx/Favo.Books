# 0006: Explainable book recommendations, first version

Status: Accepted, 2026-09-16.

## Product behavior

Signed-in readers see six recommendations on home and profile. Preferences allow
up to five genres and three equally important criteria, persisted per account.
Clearing all selections resets explicit preferences. Guests retain the landing
page. No movie/series scope is introduced.

Only books with a cover attached to a Turkish edition are eligible. Read/Reading
entries and books with the reader's detailed vote are excluded. WantToRead entries
remain eligible because they are possible next reads. Books scored at least 7/10
overall supply positive genre and author similarity. A high criterion score for a
book never automatically becomes the reader's criterion preference.

## Ranking and explanations

Candidate score sums: 4 for a chosen genre, 2 for a genre shared with a liked
book, 3 for a shared author, plus adjusted overall quality divided by 10. Each
chosen criterion contributes (adjusted criterion average - 5) divided by the
number of selected criteria, only when at least three detailed votes exist.
Adjusted averages add five neutral 5/10 prior votes. Legacy votes are excluded
from these calculations. These constants are initial heuristics, not learned or
validated predictions. They do not change public book ratings or their weights.

Positive reasons name a matching genre, a liked book or a criterion average
(with sample size). A criterion reason requires an unadjusted average of at least
7. Unmatched candidates say they are discovery suggestions; limited rating
evidence is visible. Results sort before Take(6), with BookId as a stable tie-break.
No fabricated match percentage or claim of collaborative filtering is shown.

## Boundaries and persistence

Community owns preferences and ranking. Catalog supplies IDiscoveryCatalog;
Reading supplies IReadingHistory. No cross-module DbContext access. Authentication
supplies UserId; request bodies cannot select another account. GET endpoints use
no-store. Preference writes validate keys, categories, counts and duplicates,
and use PostgreSQL upsert to avoid concurrent-first-save collisions.

## Limits and next work

The current catalog is small (~1,100 books). This version reads eligible catalog
metadata and detailed votes into memory. Before growth, aggregate rating evidence
in the database, introduce bounded candidate retrieval/read models, and measure
latency. Cold-start tie order is intentionally stable, not random or editorial.
Recommendation diversity, offline evaluation and collaborative signals remain
future work. Imported genre/author quality limits matching quality.

## Feedback extension

Per-book interested/dismissed feedback is stored independently of ratings and
reading status, keyed by the authenticated user and book. Interest adds at most
one ranking point to other books sharing a genre or author and names that source
in the explanation. It is weaker than a positive completed-book rating. Dismissal
only excludes the chosen book, without inferring dislike of its genre/author.
Changing a choice upserts the same row. DELETE is idempotent and caller-scoped.
The response includes feedback history, permitting undo even for books no longer
eligible or present in Catalog. Invalid choices and unavailable books cannot be
saved. Clearing feedback does not guarantee top-six placement; ordinary ranking
and read/rated exclusions still apply.

Tests cover account isolation, persistence, invalid criteria, Turkish-cover
eligibility, started-book exclusion, preference-sensitive order, sparse evidence,
genre/author explanations and deterministic limits. Browser checks cover save,
reload and responsive layout; automated browser E2E coverage is still pending.
