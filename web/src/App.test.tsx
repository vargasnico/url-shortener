import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'

// Substitui o fetch do navegador por uma resposta fixa: testa o front sem a API rodando.
function mockFetch(status: number, body: unknown) {
  // Uma Response nova por chamada: o corpo de uma Response só pode ser lido uma vez.
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async () =>
    new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }),
  )
}

const createdLink = {
  code: 'aB3xK9',
  shortUrl: 'http://localhost:5259/aB3xK9',
  originalUrl: 'https://example.com/long',
  createdAt: '2026-10-29T12:00:00Z',
}

describe('App', () => {
  it('shortens a URL and shows it in the recent links', async () => {
    const fetchMock = mockFetch(201, createdLink)
    render(<App />)

    await userEvent.type(screen.getByLabelText('URL to shorten'), 'https://example.com/long')
    await userEvent.click(screen.getByRole('button', { name: 'Shorten' }))

    expect(await screen.findByRole('link', { name: createdLink.shortUrl })).toBeInTheDocument()
    expect(screen.getByText(createdLink.originalUrl)).toBeInTheDocument()
    expect(screen.getByLabelText('URL to shorten')).toHaveValue('')
    expect(fetchMock).toHaveBeenCalledWith('/links', expect.objectContaining({ method: 'POST' }))
  })

  it('shows the validation message returned by the API', async () => {
    mockFetch(400, { title: 'One or more validation errors occurred.', errors: { Url: ['Provide an absolute URL.'] } })
    render(<App />)

    await userEvent.type(screen.getByLabelText('URL to shorten'), 'not a url')
    await userEvent.click(screen.getByRole('button', { name: 'Shorten' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Provide an absolute URL.')
    expect(screen.queryByRole('heading', { name: 'Recent links' })).not.toBeInTheDocument()
  })

  it('disables the button while the input is empty', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: 'Shorten' })).toBeDisabled()
  })

  it('restores recent links from previous visits', () => {
    localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
    render(<App />)

    expect(screen.getByRole('link', { name: createdLink.shortUrl })).toBeInTheDocument()
  })

  it('clears the recent links', async () => {
    localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
    render(<App />)

    await userEvent.click(screen.getByRole('button', { name: 'Clear' }))

    expect(screen.queryByRole('link', { name: createdLink.shortUrl })).not.toBeInTheDocument()
  })

  describe('stats', () => {
    const today = new Date().toISOString().slice(0, 10)
    // O número fica num <strong> separado, então buscamos pela palavra e conferimos o texto do parágrafo inteiro.
    const findTotal = () => screen.findByText(/clicks?$/, { selector: '.stats-total' })

    it('shows the total and the daily clicks of a link', async () => {
      localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
      const fetchMock = mockFetch(200, { code: createdLink.code, totalClicks: 5, daily: [{ date: today, clicks: 5 }] })
      render(<App />)

      await userEvent.click(screen.getByRole('button', { name: 'Stats' }))

      expect(await findTotal()).toHaveTextContent('5 clicks')
      // 7 dias no gráfico, completando com zero os dias sem cliques.
      const days = within(screen.getByRole('list', { name: 'Clicks in the last 7 days' })).getAllByRole('listitem')
      expect(days).toHaveLength(7)
      expect(days[6]).toHaveTextContent(': 5')
      expect(days[0]).toHaveTextContent(': 0')
      expect(fetchMock).toHaveBeenCalledWith('/links/aB3xK9/stats')
      expect(screen.getByRole('button', { name: 'Stats' })).toHaveAttribute('aria-expanded', 'true')
    })

    it('shows an error when the link no longer exists', async () => {
      localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
      mockFetch(404, {})
      render(<App />)

      await userEvent.click(screen.getByRole('button', { name: 'Stats' }))

      expect(await screen.findByRole('alert')).toHaveTextContent('Link not found.')
    })

    it('reloads the stats on refresh', async () => {
      localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
      const fetchMock = mockFetch(200, { code: createdLink.code, totalClicks: 0, daily: [] })
      render(<App />)

      await userEvent.click(screen.getByRole('button', { name: 'Stats' }))
      await userEvent.click(await screen.findByRole('button', { name: 'Refresh' }))

      await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    })

    it('hides the stats when clicked again', async () => {
      localStorage.setItem('url-shortener:recent-links', JSON.stringify([createdLink]))
      mockFetch(200, { code: createdLink.code, totalClicks: 1, daily: [{ date: today, clicks: 1 }] })
      render(<App />)

      await userEvent.click(screen.getByRole('button', { name: 'Stats' }))
      expect(await findTotal()).toHaveTextContent('1 click')
      await userEvent.click(screen.getByRole('button', { name: 'Stats' }))

      expect(screen.queryByText(/clicks?$/)).not.toBeInTheDocument()
    })
  })
})
