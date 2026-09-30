# Catalog coverage snapshot

`dotnet run --project tools/Favo.Books.CatalogCoverage -c Release -- <new-report.json>`

Uses API user secrets/environment settings. Runs a read-only database transaction;
exports only catalog titles, authors, edition titles, covered-Turkish-edition flag,
latest editorial decision and Open Library work IDs. Does not read user records.
Refuses to overwrite reports. Keep snapshots under ignored `artifacts/`.
