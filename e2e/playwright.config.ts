import { defineConfig, devices } from '@playwright/test'

// Sobe a API e o front antes dos testes (ou reaproveita, se já estiverem rodando).
// O PostgreSQL precisa estar no ar: `docker compose up -d` na raiz do projeto.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../src/UrlShortener.Api --launch-profile http',
      url: 'http://localhost:5259/health',
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npm run dev --prefix ../web',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
    },
  ],
})
