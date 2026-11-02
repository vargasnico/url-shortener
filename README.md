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

## Deploying to AWS

The stack is defined with [AWS SAM](https://docs.aws.amazon.com/serverless-application-model/) in `infra/template.yaml`:

| Component | AWS service |
|---|---|
| Front end | S3 (private bucket) + CloudFront |
| API | Lambda (.NET 10) behind an API Gateway HTTP API, routed through CloudFront |
| Stats worker | Lambda (Python 3.13) triggered every minute by EventBridge Scheduler |
| Database | RDS for PostgreSQL 17, credentials in Secrets Manager |

CloudFront serves everything from one domain: `/links*`, `/health` and 6-character short codes go to the API, everything else to the front end.
The API applies migrations on startup (`Database__MigrateOnStartup=true`).

Requirements: [AWS CLI](https://aws.amazon.com/cli/) with credentials configured, [SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html), Docker, .NET 10 SDK, Node.js and uv.

```powershell
.\infra\deploy.ps1                       # builds, deploys the stack, uploads the front end
.\infra\destroy.ps1                      # deletes everything (keeps a final database snapshot)
```

The first deploy takes around 10 minutes because of the database. The RDS instance runs continuously and is the main cost of the stack.

## Deploying to Azure (Azure for Students)

An alternative stack that needs no credit card: an [Azure for Students](https://azure.microsoft.com/free/students) subscription gives US$100 of credit for 12 months.
It is defined with [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/) in `infra/azure/main.bicep`:

| Component | Azure service | Cost |
|---|---|---|
| API + front end | App Service, Free (F1) Linux plan; the API serves the Vite build from `wwwroot` | Free |
| Stats worker | Azure Functions, Flex Consumption (Python 3.13), timer trigger every minute | Within the free monthly grant |
| Database | Azure Database for PostgreSQL Flexible Server 17, Burstable B1MS, 32 GB | Uses credit when not covered by a free offer |
| Function storage | Storage account (timer state and deployment package) | A few cents |

Requirements: [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), .NET 10 SDK, Node.js and uv.

```powershell
az login                                 # sign in with the Azure for Students account
.\infra\azure\deploy.ps1                 # creates everything in brazilsouth and prints the site URL
.\infra\azure\deploy.ps1 -Location eastus2   # if your subscription does not allow brazilsouth
.\infra\azure\destroy.ps1                # deletes the resource group, database included
```

The first deploy takes around 10 minutes because of the database. Each deploy generates a new database password and updates the API and the worker with it.

Limits of the free plan: the App Service F1 plan has 60 CPU minutes per day and no "Always On", so the first request after ~20 minutes idle takes a few seconds.
The database firewall allows connections from Azure services (needed without a virtual network); access still requires the password.
To save credit when you are not using it, stop the database with `az postgres flexible-server stop` (Azure starts it again automatically after 7 days).
