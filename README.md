# URL Shortener

A URL shortener API built with ASP.NET Core (.NET 10), Entity Framework Core and PostgreSQL.

## Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/links` | Shortens a URL. Body: `{ "url": "https://example.com" }` → `201 Created` |
| `GET` | `/links/{code}` | Returns the link details |
| `GET` | `/{code}` | Redirects (`302`) to the original URL |
| `GET` | `/health` | Health check |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

```bash
docker compose up -d                      # PostgreSQL on localhost:5432
dotnet run --project src/UrlShortener.Api
```

The API listens on `http://localhost:5259` and applies pending migrations on startup in Development.
Example requests are in `src/UrlShortener.Api/UrlShortener.Api.http`.

### Migrations

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add <Name> --project src/UrlShortener.Api --output-dir Data/Migrations
```

## Tests

```bash
dotnet test
```

Integration tests start a disposable PostgreSQL container with [Testcontainers](https://dotnet.testcontainers.org/), so Docker must be running.
