import { useCallback, useState } from 'react'
import { listDeletedProviders, restoreProvider } from '../api/providers.js'
import { useApi } from '../hooks/useApi.js'
import { formatDateTime } from '../utils/format.js'
import { Card, EmptyState, ErrorMessage, Notice, PageHeader, SkeletonRows, StatusBadge } from './common.jsx'

/** Audit view of soft-deleted providers. They are still in the database and can be restored. */
export default function DeletedProviders() {
  const { data: providers, error, loading, reload } = useApi(listDeletedProviders)
  const [restoringId, setRestoringId] = useState(null)
  const [actionError, setActionError] = useState(null)
  const [notice, setNotice] = useState(null)
  const dismissNotice = useCallback(() => setNotice(null), [])

  async function handleRestore(provider) {
    setRestoringId(provider.providerId)
    setActionError(null)
    setNotice(null)
    try {
      await restoreProvider(provider.providerId)
      setNotice(`${provider.providerName} was restored and is back in the provider list.`)
      reload()
    } catch (err) {
      setActionError(err)
    } finally {
      setRestoringId(null)
    }
  }

  return (
    <>
      <PageHeader
        title="Deleted providers"
        description="Soft-deleted records are hidden from every standard list and report, but kept for audit and troubleshooting."
      />

      <Notice message={notice} onDismiss={dismissNotice} />
      <ErrorMessage error={actionError} />

      <Card title="Soft-deleted" titleId="deleted-heading" count={providers?.length}>
        <ErrorMessage error={error} onRetry={reload} />
        {loading && !providers && <SkeletonRows rows={3} />}
        {providers?.length === 0 && <EmptyState title="Nothing deleted">No providers have been deleted.</EmptyState>}

        {providers?.length > 0 && (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Provider</th>
                  <th scope="col">County</th>
                  <th scope="col">Status when deleted</th>
                  <th scope="col">Deleted</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody aria-busy={loading}>
                {providers.map((provider) => (
                  <tr key={provider.providerId}>
                    <td>{provider.providerName}</td>
                    <td>{provider.county}</td>
                    <td>
                      <StatusBadge status={provider.status} />
                    </td>
                    <td>
                      {formatDateTime(provider.deletedDate)}
                      <div className="muted small">by {provider.deletedBy ?? 'unknown'}</div>
                    </td>
                    <td className="num">
                      <button type="button" onClick={() => handleRestore(provider)} disabled={restoringId !== null}>
                        {restoringId === provider.providerId ? 'Restoring…' : 'Restore'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  )
}
