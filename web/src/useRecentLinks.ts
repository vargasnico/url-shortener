import { useEffect, useState } from 'react'
import type { Link } from './api'

const STORAGE_KEY = 'url-shortener:recent-links'
const MAX_LINKS = 10

// Histórico dos links criados neste navegador. Fica no localStorage, não no servidor.
export function useRecentLinks() {
  const [links, setLinks] = useState<Link[]>(load)

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(links))
    } catch {
      // Modo privado ou armazenamento bloqueado: o histórico só não persiste.
    }
  }, [links])

  const add = (link: Link) =>
    setLinks((current) => [link, ...current.filter((l) => l.code !== link.code)].slice(0, MAX_LINKS))

  const clear = () => setLinks([])

  return { links, add, clear }
}

function load(): Link[] {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved ? JSON.parse(saved) : []
  } catch {
    return []
  }
}
