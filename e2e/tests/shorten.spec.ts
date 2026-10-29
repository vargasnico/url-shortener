import { expect, test } from '@playwright/test'

test('shortens a URL and the short link redirects to it', async ({ page, request }) => {
  const target = `https://example.com/e2e/${Date.now()}`

  await page.goto('/')
  await page.getByLabel('URL to shorten').fill(target)
  await page.getByRole('button', { name: 'Shorten' }).click()

  const shortLink = page.getByRole('link', { name: /\/[a-zA-Z0-9]{6}$/ }).first()
  await expect(shortLink).toBeVisible()
  await expect(page.getByText(target)).toBeVisible()

  // Confere o redirecionamento direto na API, sem seguir para o site externo.
  const shortUrl = await shortLink.getAttribute('href')
  const response = await request.get(shortUrl!, { maxRedirects: 0 })
  expect(response.status()).toBe(302)
  expect(response.headers()['location']).toBe(target)
})

test('shows an error for an invalid URL', async ({ page }) => {
  await page.goto('/')
  await page.getByLabel('URL to shorten').fill('not-a-url')
  await page.getByRole('button', { name: 'Shorten' }).click()

  await expect(page.getByRole('alert')).toContainText('http://')
})
