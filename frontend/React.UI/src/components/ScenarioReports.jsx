import { getActiveProvidersActiveLicenses, getActiveProvidersExpiredLicenses } from '../api/reports.js'
import { useApi } from '../hooks/useApi.js'
import { describeDays, formatDate } from '../utils/format.js'
import { Card, EmptyState, ErrorMessage, PageHeader, SkeletonRows } from './common.jsx'

/** The two required data scenarios. Both lists come from database views through the API. */
export default function ScenarioReports() {
  return (
    <>
      <PageHeader
        title="License scenarios"
        description="The two required license reports. Deleted providers are excluded."
      />
      <div className="stack">
        <ActiveLicenses />
        <ExpiredLicenses />
      </div>
    </>
  )
}

function ActiveLicenses() {
  const { data: rows, error, loading, reload } = useApi(getActiveProvidersActiveLicenses)

  return (
    <Card
      title="Active providers and their active licenses"
      titleId="active-heading"
      count={rows?.length}
      description="Each valid license held by a provider with status Active."
    >
      <ErrorMessage error={error} onRetry={reload} />
      {loading && !rows && <SkeletonRows rows={4} />}
      {rows?.length === 0 && <EmptyState title="No active licenses">No active provider holds a valid license.</EmptyState>}
      {rows?.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th scope="col">Provider</th>
                <th scope="col">County</th>
                <th scope="col">License</th>
                <th scope="col">Expires</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.licenseId}>
                  <td>{row.providerName}</td>
                  <td>{row.county}</td>
                  <td className="mono">{row.licenseNumber}</td>
                  <td>
                    {formatDate(row.expirationDate)}
                    <div className="muted small">{describeDays(row.daysUntilExpiration)}</div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  )
}

function ExpiredLicenses() {
  const { data: rows, error, loading, reload } = useApi(getActiveProvidersExpiredLicenses)

  return (
    <Card
      title="Active providers with expired licenses"
      titleId="expired-heading"
      count={rows?.length}
      description="Licenses past their expiration date, held by providers still marked Active."
    >
      <ErrorMessage error={error} onRetry={reload} />
      {loading && !rows && <SkeletonRows rows={3} />}
      {rows?.length === 0 && <EmptyState title="All clear">No active provider has an expired license.</EmptyState>}
      {rows?.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th scope="col">Provider</th>
                <th scope="col">County</th>
                <th scope="col">License</th>
                <th scope="col">Expired</th>
                <th scope="col">Recorded status</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.licenseId}>
                  <td>{row.providerName}</td>
                  <td>{row.county}</td>
                  <td className="mono">{row.licenseNumber}</td>
                  <td>
                    {formatDate(row.expirationDate)}
                    <div className="muted small">{describeDays(-row.daysSinceExpiration)}</div>
                  </td>
                  <td>
                    {row.recordedStatus}
                    {row.isStatusStale && <div className="hint-warning">Needs update</div>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  )
}
