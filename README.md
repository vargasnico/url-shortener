# URL Shortener

A URL shortener with an ASP.NET Core (.NET 10) API, PostgreSQL via Entity Framework Core, a React + TypeScript front end, and a Python worker that aggregates click statistics.

## Structure

| Folder | Description |
|---|---|
| `src/UrlShortener.Api` | ASP.NET Core Web API |
| `tests/UrlShortener.Api.Tests` | API integration tests (xUnit + Testcontainers) |
| `web/` | React + TypeScript front end (Vite) |
| `worker/` | Python worker that aggregates click events into daily stats |
| `e2e/` | End-to-end tests (Playwright) |

## Click statistics

Every redirect records a row in `click_events`. The stats worker periodically aggregates pending events into `link_daily_stats` (one row per link per UTC day) and marks them as processed, so `GET /links/{code}/stats` reads small, pre-computed totals. Stats are eventually consistent: new clicks show up after the next worker run (every 10 seconds by default).

The database schema is owned by the API's EF Core migrations; the worker only reads and writes rows.

## API endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/links` | Shortens a URL. Body: `{ "url": "https://example.com" }` → `201 Created` |
| `GET` | `/links/{code}` | Returns the link details |
| `GET` | `/links/{code}/stats` | Returns total and daily click counts |
| `GET` | `/{code}` | Redirects (`302`) to the original URL |
| `GET` | `/health` | Health check |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 22+, [uv](https://docs.astral.sh/uv/) and Docker.

```bash
docker compose up -d                       # PostgreSQL on localhost:5432 + stats worker
dotnet run --project src/UrlShortener.Api  # API on http://localhost:5259
cd web && npm install && npm run dev       # front end on http://localhost:5173
```

The API applies pending migrations on startup in Development. In development, Vite proxies `/links` requests to the API, so no CORS setup is needed.
Example API requests are in `src/UrlShortener.Api/UrlShortener.Api.http`.

### Migrations

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add <Name> --project src/UrlShortener.Api --output-dir Data/Migrations
# keep the worker's test schema in sync:
dotnet ef migrations script --project src/UrlShortener.Api -o worker/tests/schema.sql
```

### Running the worker outside Docker

```bash
cd worker
uv sync
uv run stats-worker          # runs every INTERVAL_SECONDS (default 10)
uv run stats-worker --once   # processes pending events once and exits
```

Configured with `DATABASE_URL`, `INTERVAL_SECONDS` and `BATCH_SIZE` environment variables.

## Tests

```bash
dotnet test                       # API integration tests (needs Docker)
cd web && npm test                # front-end component tests (Vitest)
cd worker && uv run pytest        # worker tests (needs Docker)
cd e2e && npm install && npx playwright install chromium && npm test
```

The end-to-end tests start the API and the front end automatically; PostgreSQL must already be running (`docker compose up -d`).
