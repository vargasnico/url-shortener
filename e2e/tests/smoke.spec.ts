import { test, expect } from '@playwright/test';

// Teste de fumaça: só confirma que o Playwright e o Chromium funcionam.
// Troque pelos testes do seu front React quando ele existir.
test('playwright está funcionando', async ({ page }) => {
  await page.setContent('<h1>Olá, Playwright</h1>');
  await expect(page.getByRole('heading')).toHaveText('Olá, Playwright');
});
