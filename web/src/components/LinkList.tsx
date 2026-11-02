import { useState } from 'react'
import type { Link } from '../api'
import { CopyButton } from './CopyButton'
import { LinkStats } from './LinkStats'

interface Props {
  links: Link[]
  onClear: () => void
}

export function LinkList({ links, onClear }: Props) {
  // Só um painel de estatísticas aberto por vez.
  const [openCode, setOpenCode] = useState<string | null>(null)

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
        {links.map((link) => {
          const open = openCode === link.code
          return (
            <li key={link.code}>
              <div className="link-row">
                <div className="link-text">
                  <a href={link.shortUrl} target="_blank" rel="noreferrer">
                    {link.shortUrl}
                  </a>
                  <span className="original" title={link.originalUrl}>
                    {link.originalUrl}
                  </span>
                </div>
                <button
                  type="button"
                  className="secondary"
                  aria-expanded={open}
                  aria-controls={`stats-${link.code}`}
                  onClick={() => setOpenCode(open ? null : link.code)}
                >
                  Stats
                </button>
                <CopyButton text={link.shortUrl} />
              </div>
              {open && (
                <div id={`stats-${link.code}`}>
                  <LinkStats code={link.code} />
                </div>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}
