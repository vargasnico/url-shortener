// Formato devolvido pela API (LinkResponse no C#; o ASP.NET Core serializa em camelCase).
export interface Link {
  code: string
  shortUrl: string
  originalUrl: string
  createdAt: string
}

// LinkStatsResponse no C#. "daily" só traz os dias que tiveram cliques (date no formato YYYY-MM-DD, UTC).
export interface LinkStats {
  code: string
  totalClicks: number
  daily: { date: string; clicks: number }[]
}

// Formato de erro padrão do ASP.NET Core (RFC 9457 "problem details").
interface ProblemDetails {
  title?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {}

export async function createLink(url: string): Promise<Link> {
  const response = await fetch('/links', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url }),
  })

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response))
  }

  return response.json()
}

export async function getLinkStats(code: string): Promise<LinkStats> {
  const response = await fetch(`/links/${encodeURIComponent(code)}/stats`)

  if (!response.ok) {
    throw new ApiError(response.status === 404 ? 'Link not found.' : await readErrorMessage(response))
  }

  return response.json()
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem: ProblemDetails = await response.json()
    // Erros de validação vêm agrupados por campo; mostramos o primeiro.
    const firstError = Object.values(problem.errors ?? {}).flat()[0]
    return firstError ?? problem.title ?? `Request failed (${response.status})`
  } catch {
    return `Request failed (${response.status})`
  }
}
