# URL Shortener

URL shortener: ASP.NET Core (.NET 10) API with PostgreSQL (EF Core) and a React front end.

## Structure

- `src/UrlShortener.Api` — ASP.NET Core Web API (controllers, EF Core)
- `tests/UrlShortener.Api.Tests` — xUnit integration tests with `WebApplicationFactory` + Testcontainers
- `web/` — React + TypeScript (Vite), Vitest + Testing Library
- `e2e/` — Playwright end-to-end tests

## Conventions

- Code, routes, JSON fields and API messages in English.
- Code comments in Portuguese.
- Commit messages in English, imperative mood ("Add ...", "Fix ...").

## Commands

```bash
docker compose up -d                        # PostgreSQL
dotnet build
dotnet test
dotnet run --project src/UrlShortener.Api   # http://localhost:5259
cd web && npm run dev                       # http://localhost:5173
cd web && npm test && npm run lint
cd e2e && npx playwright test
```
