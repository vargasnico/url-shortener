# URL Shortener

A URL shortener API built with ASP.NET Core (.NET 10).

## Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/links` | Shortens a URL. Body: `{ "url": "https://example.com" }` → `201 Created` |
| `GET` | `/links/{code}` | Returns the link details |
| `GET` | `/{code}` | Redirects (`302`) to the original URL |
| `GET` | `/health` | Health check |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/UrlShortener.Api
```

The API listens on `http://localhost:5259`. Example requests are in `src/UrlShortener.Api/UrlShortener.Api.http`.

## Tests

```bash
dotnet test
```
