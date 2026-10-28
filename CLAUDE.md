# URL Shortener

URL shortener API built with ASP.NET Core (.NET 10).

## Structure

- `src/UrlShortener.Api` — ASP.NET Core Web API (controllers)
- `tests/UrlShortener.Api.Tests` — xUnit integration tests with `WebApplicationFactory`
- `e2e/` — Playwright end-to-end tests

## Conventions

- Code, routes, JSON fields and API messages in English.
- Code comments in Portuguese.
- Commit messages in English, imperative mood ("Add ...", "Fix ...").

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/UrlShortener.Api   # http://localhost:5259
cd e2e && npx playwright test
```
