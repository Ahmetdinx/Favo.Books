# Favo Kitap

Favo is the umbrella brand; this repository contains its book application,
named `Favo.Books` in the .NET solution. See [brand transition](docs/favo-brand-transition.md)
for compatibility notes and the next steps toward the shared Favo entry point.

Favo.Books is evolving into personalized novel discovery and a shareable virtual
library, limited to novels with covered Turkish editions. Ratings and recommendations
exist; novel eligibility, nicknames, public/private profiles and achievements are
planned. Posts, messages and comment threads are out of scope.
It does not host book files or provide in-app reading.

See the [revised product plan](docs/novel-product-plan.md) and [roadmap](ROADMAP.md).
Planned behavior is not yet implemented.

New book imports now require source-reviewed novel identity and a covered Turkish
edition; free-form book creation is closed. The five-entry curated seed is ordered
editorially, not by measured readership. Existing catalog visibility has not yet
been filtered to reviewed novels. See [import policy](docs/novel-import-policy.md).

## Current status

Updated 2026-09-16. Implemented in code:

- Modular .NET 10 API, PostgreSQL persistence, exception handling and ProblemDetails.
- Registration, JWT login, refresh-token sessions and logout.
- Local catalog browsing, book details, categories, authors and editions.
- Open Library search/details, local resolution and background import jobs.
- Turkish-edition selection for newly resolved external books.
- Personal library, five reading states and reading summary.
- Profile bedside book and up to five influential books, with personal notes.
- Community: seven required 1-10 criterion scores per user/book, weighted overall,
  criterion averages, vote count, editing and removal.
- Catalog existence validation before library/profile book writes.
- React/TypeScript catalog, book details, authentication and private profile pages.

The web catalog currently searches local records. External discovery exists in
backend APIs but is not yet connected to a complete user-facing discovery flow.
Community reviews and comments are excluded from the revised scope. Public deployment and
browser E2E verification remain pending. Existing invalid book references are not
repaired automatically.

## Development

Windows: run `./Start-Favo.ps1` from the repository to start the API and web app.
The launcher resolves the current folder automatically and waits for the API.
Keep PostgreSQL running as described below.

Use .NET SDK 10.0.302 (see global.json), Node.js/npm compatible with the frontend
lockfile, and Docker with Compose for PostgreSQL and integration tests. Node
24.19.0 was used during the project review.

From the repository root:

1. Create an untracked `.env` with `POSTGRES_DB`, `POSTGRES_USER`, and
   `POSTGRES_PASSWORD`. These values configure Compose, not the API.
2. Run `docker compose up -d postgres`.
3. Configure the API with .NET user secrets or environment variables. Required
   keys are `ConnectionStrings:Database`, `Jwt:Issuer`, `Jwt:Audience`,
   `Jwt:Secret`, `Jwt:ExpirationMinutes`, `RefreshToken:ExpirationDays`, and
   `RefreshToken:CookieName`. Use a strong JWT secret of at least 32 bytes.
   Environment variables use double underscores, e.g. `Jwt__Secret`.
   The database connection must match your Compose credentials.
4. Run `dotnet restore Favo.Books.slnx` and apply the module migrations described
   below. The API does not automatically migrate the development database.
5. Run `dotnet run --project src/backend/Api/Favo.Books.Api --launch-profile http`.
6. In a second terminal, run `npm ci` then `npm run dev` in `src/frontend`.

The API listens at `http://localhost:5117`; Vite proxies `/api` to this address.
Swagger is available at `/swagger` in Development and health at `/health`.
The health endpoint currently confirms application health, not database readiness.
Open Library options use `Catalog:OpenLibrary`; background worker options use
`Catalog:ImportJobs`. Disable the worker with
`Catalog__ImportJobs__WorkerEnabled=false` when background imports are unwanted.

### Database migrations

Install a compatible .NET 10 `dotnet-ef` tool if it is not already available.
Run the following from the repository root with Development configuration and
API secrets available. In PowerShell, set `$env:ASPNETCORE_ENVIRONMENT='Development'`.

```powershell
dotnet ef database update --project src/backend/Modules/Identity/Favo.Books.Modules.Identity --startup-project src/backend/Api/Favo.Books.Api --context IdentityDbContext
dotnet ef database update --project src/backend/Modules/Catalog/Favo.Books.Modules.Catalog --startup-project src/backend/Api/Favo.Books.Api --context CatalogDbContext
dotnet ef database update --project src/backend/Modules/Reading/Favo.Books.Modules.Reading --startup-project src/backend/Api/Favo.Books.Api --context ReadingDbContext
dotnet ef database update --project src/backend/Modules/Profiles/Favo.Books.Modules.Profiles --startup-project src/backend/Api/Favo.Books.Api --context ProfilesDbContext
dotnet ef database update --project src/backend/Modules/Community/Favo.Books.Modules.Community --startup-project src/backend/Api/Favo.Books.Api --context CommunityDbContext
```

The current Catalog migration clears stored cover URLs for every non-Turkish
edition (`LanguageCode` other than `tr`/`tur`). This cleanup is intentionally
irreversible because the removed external URLs cannot be reconstructed reliably.
That migration leaves book and edition rows in place. The separate, explicitly run
[catalog cleanup tool](tools/Favo.Books.Maintenance/README.md) removes books with no
Turkish edition containing a cover, archives affected records and clears linked
library/profile/rating records in one transaction. It is not run on startup.

The Community detailed-rating migration preserves old 1-5 votes as legacy data.
They are excluded from new averages until users submit all seven criterion scores.
See [ADR 0005](docs/decisions/0005-detailed-book-ratings.md) for weights and contracts.

### Verification

```powershell
dotnet test Favo.Books.slnx
```

Integration tests use isolated PostgreSQL Testcontainers and require a running
Docker engine. They do not use the development database. The test image is
currently PostgreSQL 17, while Compose uses 18.4; aligning these remains pending.

When Docker is unavailable, run only architecture and unit tests:

```powershell
dotnet test Favo.Books.slnx --filter 'FullyQualifiedName!~Favo.Books.Api.IntegrationTests'
```

In `src/frontend`, run `npm run build`, `npm run lint` and `npm run test:ratings` (Node 24).
Browser E2E tests have
not yet been added. Passing these commands does not verify live external data.

## Project guide

- [Current context and next task](PROJECT_CONTEXT.md)
- [Roadmap](ROADMAP.md)
- [Architecture](ARCHITECTURE.md)
- [Architecture decisions](docs/decisions/)

Open Library is the implemented external provider. Wikidata enrichment and Google
Books fallback are planned, not implemented. User following, likes, feeds,
notifications, and a dedicated administration panel remain outside the initial MVP.
