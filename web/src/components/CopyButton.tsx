import { useEffect, useState } from 'react'

export function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false)

  // Volta o texto do botão para "Copy" depois de 2 segundos.
  useEffect(() => {
    if (!copied) return
    const timer = setTimeout(() => setCopied(false), 2000)
    return () => clearTimeout(timer)
  }, [copied])

  async function copy() {
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
    } catch {
      // Sem permissão de clipboard: o usuário ainda pode selecionar o link manualmente.
    }
  }

  return (
    <button type="button" className="secondary" onClick={copy}>
      {copied ? 'Copied!' : 'Copy'}
    </button>
  )
}
