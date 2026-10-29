import { LinkList } from './components/LinkList'
import { ShortenForm } from './components/ShortenForm'
import { useRecentLinks } from './useRecentLinks'

export default function App() {
  const { links, add, clear } = useRecentLinks()

  return (
    <main className="container">
      <header>
        <h1>URL Shortener</h1>
        <p className="subtitle">Paste a long URL and get a short link to share.</p>
      </header>
      <ShortenForm onCreated={add} />
      <LinkList links={links} onClear={clear} />
    </main>
  )
}
