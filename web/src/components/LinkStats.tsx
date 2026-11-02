import { useEffect, useState } from 'react'
import { getLinkStats, type LinkStats as Stats } from '../api'

const DAYS_SHOWN = 7

export function LinkStats({ code }: { code: string }) {
  const [stats, setStats] = useState<Stats | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  // Incrementar este contador dispara uma nova busca (botão Refresh).
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    // Ignora a resposta se o painel fechar (ou o código mudar) antes de ela chegar.
    let cancelled = false
    getLinkStats(code)
      .then(
        (result) => {
          if (!cancelled) setStats(result)
        },
        (err) => {
          if (!cancelled) setError(err instanceof Error ? err.message : 'Something went wrong.')
        },
      )
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [code, reloadKey])

  function load() {
    setLoading(true)
    setError(null)
    setReloadKey((key) => key + 1)
  }

  const days = stats ? lastDays(stats.daily, DAYS_SHOWN) : []
  const max = Math.max(1, ...days.map((d) => d.clicks))

  return (
    <div className="stats" aria-busy={loading}>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {stats && (
        <>
          <p className="stats-total">
            <strong>{stats.totalClicks}</strong> {stats.totalClicks === 1 ? 'click' : 'clicks'}
          </p>
          <ol className="stats-chart" aria-label={`Clicks in the last ${DAYS_SHOWN} days`}>
            {days.map((day) => (
              <li key={day.date} title={`${day.date}: ${day.clicks}`}>
                <span className="bar" style={{ height: `${(day.clicks / max) * 100}%` }} />
                <span className="day">{formatDay(day.date)}</span>
                <span className="visually-hidden">: {day.clicks}</span>
              </li>
            ))}
          </ol>
        </>
      )}
      <div className="stats-footer">
        {/* O worker agrega os cliques periodicamente, então os números chegam com um pequeno atraso. */}
        <span>Updated every few seconds.</span>
        <button type="button" className="link-button" onClick={load} disabled={loading}>
          {loading ? 'Loading…' : 'Refresh'}
        </button>
      </div>
    </div>
  )
}

// A API só devolve dias com cliques; aqui completamos os últimos N dias (UTC) com zero.
function lastDays(daily: Stats['daily'], count: number) {
  const byDate = new Map(daily.map((d) => [d.date, d.clicks]))
  const today = new Date()
  return Array.from({ length: count }, (_, i) => {
    const date = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() - (count - 1 - i)))
    const key = date.toISOString().slice(0, 10)
    return { date: key, clicks: byDate.get(key) ?? 0 }
  })
}

function formatDay(date: string) {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-US', { weekday: 'short', timeZone: 'UTC' })
}
