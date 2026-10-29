import type { Link } from '../api'
import { CopyButton } from './CopyButton'

interface Props {
  links: Link[]
  onClear: () => void
}

export function LinkList({ links, onClear }: Props) {
  if (links.length === 0) return null

  return (
    <section className="recent" aria-labelledby="recent-title">
      <div className="recent-header">
        <h2 id="recent-title">Recent links</h2>
        <button type="button" className="link-button" onClick={onClear}>
          Clear
        </button>
      </div>
      <ul>
        {links.map((link) => (
          <li key={link.code}>
            <div className="link-text">
              <a href={link.shortUrl} target="_blank" rel="noreferrer">
                {link.shortUrl}
              </a>
              <span className="original" title={link.originalUrl}>
                {link.originalUrl}
              </span>
            </div>
            <CopyButton text={link.shortUrl} />
          </li>
        ))}
      </ul>
    </section>
  )
}
