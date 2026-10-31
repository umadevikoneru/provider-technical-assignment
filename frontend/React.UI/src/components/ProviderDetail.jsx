import { useCallback, useState } from 'react'
import { deleteProvider, getProvider, getProviderAudit } from '../api/providers.js'
import { useApi } from '../hooks/useApi.js'
import { describeDays, formatDate, formatDateTime, pluralize } from '../utils/format.js'
import { Card, EmptyState, ErrorMessage, SkeletonRows, StatusBadge } from './common.jsx'
import ConfirmDialog from './ConfirmDialog.jsx'

export default function ProviderDetail({ providerId, onEdit, onDeleted }) {
  const loadProvider = useCallback((signal) => getProvider(providerId, signal), [providerId])
  const { data: provider, error, loading, reload } = useApi(loadProvider)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState(null)
  const [showHistory, setShowHistory] = useState(false)

  async function handleDelete() {
    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteProvider(providerId)
      setConfirmingDelete(false)
      onDeleted(provider)
    } catch (err) {
      setDeleteError(err)
    } finally {
      setDeleting(false)
    }
  }

  function closeDeleteDialog() {
    setConfirmingDelete(false)
    setDeleteError(null)
  }

  if (error) {
    return (
      <Card>
        {/* 404 means the provider was deleted meanwhile; retrying would not help. */}
        <ErrorMessage error={error} onRetry={error.status === 404 ? undefined : reload} />
      </Card>
    )
  }

  if (loading && !provider) {
    return (
      <Card>
        <SkeletonRows rows={6} label="Loading provider" />
      </Card>
    )
  }

  return (
    <Card
      title={provider.providerName}
      titleId="detail-heading"
      actions={
        <>
          <button type="button" onClick={() => onEdit(provider)}>
            Edit
          </button>
          <button type="button" className="button-danger" onClick={() => setConfirmingDelete(true)}>
            Delete
          </button>
        </>
      }
    >
      <dl className="facts">
        <div>
          <dt>Status</dt>
          <dd>
            <StatusBadge status={provider.status} />
          </dd>
        </div>
        <div>
          <dt>County</dt>
          <dd>{provider.county}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{formatDateTime(provider.createdDate)}</dd>
        </div>
        <div>
          <dt>Last updated</dt>
          <dd>{provider.updatedDate ? `${formatDateTime(provider.updatedDate)} by ${provider.updatedBy}` : 'Never'}</dd>
        </div>
      </dl>

      <h3 className="section-title">
        Licenses <span className="count">{provider.licenses.length}</span>
      </h3>
      <LicenseTable licenses={provider.licenses} />

      <div className="section-footer">
        <button
          type="button"
          className="button-link"
          aria-expanded={showHistory}
          onClick={() => setShowHistory((value) => !value)}
        >
          {showHistory ? 'Hide change history' : 'Show change history'}
        </button>
        {/* Keyed by version so the history reloads after the provider changes. */}
        {showHistory && <AuditHistory key={provider.version} providerId={providerId} />}
      </div>

      {confirmingDelete && (
        <ConfirmDialog
          title="Delete this provider?"
          confirmLabel="Delete provider"
          busyLabel="Deleting…"
          busy={deleting}
          error={deleteError}
          onConfirm={handleDelete}
          onCancel={closeDeleteDialog}
        >
          <p>
            <strong>{provider.providerName}</strong> will be removed from provider lists, reports and the dashboard.
          </p>
          <p className="muted">
            This is a soft delete: {describeKeptRecords(provider.licenses.length)} in the database for audit, and the
            provider can be restored from the Deleted tab.
          </p>
        </ConfirmDialog>
      )}
    </Card>
  )
}

function describeKeptRecords(licenseCount) {
  if (licenseCount === 0) return 'the record stays'
  return `the record and its ${pluralize(licenseCount, 'license')} stay`
}

function LicenseTable({ licenses }) {
  if (licenses.length === 0) {
    return <EmptyState title="No licenses on file">This provider does not hold any licenses yet.</EmptyState>
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th scope="col">License number</th>
            <th scope="col">Status</th>
            <th scope="col">Expires</th>
          </tr>
        </thead>
        <tbody>
          {licenses.map((license) => (
            <tr key={license.licenseId}>
              <td className="mono">{license.licenseNumber}</td>
              <td>
                <StatusBadge status={license.effectiveStatus} />
                {license.isStatusStale && (
                  <div className="hint-warning">Still recorded as {license.recordedStatus}; needs update</div>
                )}
              </td>
              <td>
                {formatDate(license.expirationDate)}
                <div className="muted small">{describeDays(license.daysUntilExpiration)}</div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function AuditHistory({ providerId }) {
  const loadHistory = useCallback((signal) => getProviderAudit(providerId, signal), [providerId])
  const { data: entries, error, loading } = useApi(loadHistory)

  if (error) return <ErrorMessage error={error} />
  if (loading && !entries) return <SkeletonRows rows={3} label="Loading history" />

  return (
    <ol className="timeline">
      {entries.map((entry) => (
        <li key={entry.auditId}>
          <span className={`timeline-dot timeline-${entry.action.toLowerCase()}`} aria-hidden="true" />
          <div>
            <strong>{entry.action}</strong> by {entry.changedBy}
            <div className="muted small">
              {formatDateTime(entry.changedDate)} · {entry.providerName}, {entry.county}, {entry.status}
            </div>
          </div>
        </li>
      ))}
    </ol>
  )
}
