# KitapKivo

KitapKivo is a book discovery and literary community platform designed to help
readers discover meaningful books, organize their reading lives, and share
thoughtful opinions with other book lovers.

## Project Status

The project is currently under active development.

The modular .NET backend, authentication flow, React frontend foundation, and
initial Catalog use cases are complete. The next milestone is external book
discovery through Open Library.

## Initial Product Scope

- Discover books through external data sources.
- Add books to a personal library.
- Rate and review books.
- Comment on reviews.
- Receive meaningful book suggestions in later stages.

User-to-user following, likes, activity feeds, notifications, and a dedicated
administration panel are outside the initial MVP.

KitapKivo will not host copyrighted book content or provide in-app book reading.

## External Data Strategy

- Open Library is the primary book discovery and metadata source.
- Wikidata will enrich author identities and related information.
- Google Books is a secondary fallback and comparison source.
- Current Turkish editions may later be obtained through agreements with
  publishers or distributors.
- Commercial ISBN providers will only be evaluated after measuring data gaps.

## Architecture

KitapKivo uses a modular monolith with vertical slices, Clean Architecture
dependency rules, tactical Domain-Driven Design, and lightweight CQRS.

- [Architecture](ARCHITECTURE.md)
- [Roadmap](ROADMAP.md)
- [Current project context](PROJECT_CONTEXT.md)
- [Architecture decisions](docs/decisions/)

## Technology Stack

- .NET 10
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- React
- Vite