# URL Shortener

A URL shortener with an ASP.NET Core (.NET 10) API, PostgreSQL via Entity Framework Core, and a React + TypeScript front end.

## Structure

| Folder | Description |
|---|---|
| `src/UrlShortener.Api` | ASP.NET Core Web API |
| `tests/UrlShortener.Api.Tests` | API integration tests (xUnit + Testcontainers) |
| `web/` | React + TypeScript front end (Vite) |
| `e2e/` | End-to-end tests (Playwright) |

## API endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/links` | Shortens a URL. Body: `{ "url": "https://example.com" }` → `201 Created` |
| `GET` | `/links/{code}` | Returns the link details |
| `GET` | `/{code}` | Redirects (`302`) to the original URL |
| `GET` | `/health` | Health check |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 22+ and Docker.

```bash
docker compose up -d                       # PostgreSQL on localhost:5432
dotnet run --project src/UrlShortener.Api  # API on http://localhost:5259
cd web && npm install && npm run dev       # front end on http://localhost:5173
```

The API applies pending migrations on startup in Development. In development, Vite proxies `/links` requests to the API, so no CORS setup is needed.
Example API requests are in `src/UrlShortener.Api/UrlShortener.Api.http`.

### Migrations

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add <Name> --project src/UrlShortener.Api --output-dir Data/Migrations
```

## Tests

```bash
dotnet test                       # API integration tests (needs Docker)
cd web && npm test                # front-end component tests (Vitest)
cd e2e && npm install && npx playwright install chromium && npm test
```

The end-to-end tests start the API and the front end automatically; PostgreSQL must already be running (`docker compose up -d`).
