import { useCallback, useEffect, useState } from 'react'

/**
 * Runs `load(signal)` and tracks its data, error and loading state.
 *
 * Pass a stable function: a module-level API function (e.g. `getDashboard`) or one wrapped in
 * `useCallback` with its inputs as dependencies. When the function changes (new inputs) or
 * `reload()` is called, the request runs again and the previous one is aborted, so a slow,
 * stale response can never overwrite a newer one. Previous data stays visible while reloading.
 */
export function useApi(load) {
  const [version, setVersion] = useState(0)
  const [result, setResult] = useState({ load: null, version: -1, data: null, error: null })

  useEffect(() => {
    const controller = new AbortController()

    load(controller.signal).then(
      (data) => setResult({ load, version, data, error: null }),
      (error) => {
        if (error.name !== 'AbortError') setResult({ load, version, data: null, error })
      },
    )

    return () => controller.abort()
  }, [load, version])

  // Loading until the stored result belongs to the current request.
  const loading = result.load !== load || result.version !== version
  const reload = useCallback(() => setVersion((current) => current + 1), [])

  return { data: result.data, error: loading ? null : result.error, loading, reload }
}
