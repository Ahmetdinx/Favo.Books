# Catalog cleanup (2026-11-14)

- `hidden.json`: 23 duplicate records hidden from discovery (latest NovelReview `Unknown`, batch `catalog-cleanup-20261114`). `keep` is the record that stays visible. Reader data on hidden records would have moved to `keep`; none existed.
- `titles.tsv`: title fixes. Edition noise ("– Sert Kapak", "Ciltli", "– 2 Cilt", imprint names) and series suffixes ("/ Kara Kule 5") were removed from `catalog.Books.Title`. Series-first titles became "Series: Volume title" (e.g. "Labirent: Alev Deneyleri"). `Series` and `SeriesNumber` keep the series data for a later series feature. Edition titles were not changed, so search by series name still finds these books.

Local backup before the change: `artifacts/seed-20261111/work/before-cleanup-20261114.dump`.

The catalog list, book summaries and the book page now show `Book.Title` (the curated Turkish work title) instead of the newest Turkish edition title; edition titles still feed search and the edition list on the book page.
