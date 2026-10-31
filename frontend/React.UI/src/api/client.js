// Single place where the UI talks HTTP. Every API error (validation, 404, 409, network)
// becomes an ApiError so components can show it the same way.

// No authentication in this assignment; the API records this name in its audit columns.
const USER_NAME = 'web-ui'

export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Request failed (${status}).`)
    this.name = 'ApiError'
    this.status = status
    // ASP.NET Core ValidationProblemDetails: { errors: { fieldName: [messages] } }
    this.fieldErrors = problem?.errors ?? {}
  }
}

const UNREACHABLE = { title: 'Cannot reach the API. Make sure Candidate.API is running.' }
const TIMED_OUT = { title: 'The API did not respond in time. Please try again.' }

// A request that hangs longer than this is reported as an error instead of loading forever.
const TIMEOUT_MS = 15_000

export async function request(path, { method = 'GET', body, signal } = {}) {
  const headers = { Accept: 'application/json', 'X-User-Name': USER_NAME }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  // Abort on whichever comes first: the caller cancelling (stale request) or the timeout.
  const timeout = AbortSignal.timeout(TIMEOUT_MS)
  const combinedSignal = signal ? AbortSignal.any([signal, timeout]) : timeout

  let response
  let data = null
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: combinedSignal,
    })
    if (response.headers.get('content-type')?.includes('json')) {
      data = await response.json()
    }
  } catch (error) {
    if (timeout.aborted) throw new ApiError(0, TIMED_OUT)
    if (error.name === 'AbortError') throw error // cancelled by the caller; not an error to show
    throw new ApiError(0, UNREACHABLE)
  }

  if (!response.ok) {
    // The Vite proxy answers 5xx without a JSON body when the API is down.
    if (data === null && response.status >= 500) throw new ApiError(response.status, UNREACHABLE)
    throw new ApiError(response.status, data)
  }

  return data // null for 204 No Content
}
