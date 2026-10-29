import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import App from './App'

// Substitui o fetch do navegador por uma resposta fixa: testa o front sem a API rodando.
function mockFetch(status: number, body: unknown) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(
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
})
