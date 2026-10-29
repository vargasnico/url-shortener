import { useState, type FormEvent } from 'react'
import { createLink, type Link } from '../api'

interface Props {
  onCreated: (link: Link) => void
}

export function ShortenForm({ onCreated }: Props) {
  const [url, setUrl] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      onCreated(await createLink(url.trim()))
      setUrl('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="shorten-form" onSubmit={handleSubmit} noValidate>
      <label htmlFor="url" className="visually-hidden">
        URL to shorten
      </label>
      <input
        id="url"
        type="url"
        placeholder="https://example.com/a/very/long/link"
        value={url}
        onChange={(e) => setUrl(e.target.value)}
        aria-invalid={error !== null}
        aria-describedby={error ? 'url-error' : undefined}
        autoComplete="off"
        required
      />
      <button type="submit" disabled={submitting || url.trim() === ''}>
        {submitting ? 'Shortening…' : 'Shorten'}
      </button>
      {error && (
        <p id="url-error" className="error" role="alert">
          {error}
        </p>
      )}
    </form>
  )
}
