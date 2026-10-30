# URL Shortener

URL shortener: ASP.NET Core (.NET 10) API with PostgreSQL (EF Core), a React front end and a Python stats worker.

## Structure

- `src/UrlShortener.Api` — ASP.NET Core Web API (controllers, EF Core)
- `tests/UrlShortener.Api.Tests` — xUnit integration tests with `WebApplicationFactory` + Testcontainers
- `web/` — React + TypeScript (Vite), Vitest + Testing Library
- `worker/` — Python 3.13 (uv, psycopg 3) aggregating `click_events` into `link_daily_stats`; pytest + Testcontainers
- `e2e/` — Playwright end-to-end tests
- `infra/` — AWS SAM template and deploy scripts (Lambda, API Gateway, RDS, S3, CloudFront)

The schema is owned by EF Core migrations. After adding a migration, regenerate `worker/tests/schema.sql`
(`dotnet ef migrations script --project src/UrlShortener.Api -o worker/tests/schema.sql`).

## Conventions

- Code, routes, JSON fields and API messages in English.
- Code comments in Portuguese.
- Commit messages in English, imperative mood ("Add ...", "Fix ...").

## Commands

```bash
docker compose up -d                        # PostgreSQL + stats worker
dotnet build
dotnet test
dotnet run --project src/UrlShortener.Api   # http://localhost:5259
cd web && npm run dev                       # http://localhost:5173
cd web && npm test && npm run lint
cd worker && uv run pytest && uv run ruff check .
cd e2e && npx playwright test
```
