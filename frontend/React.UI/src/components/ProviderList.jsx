import { PROVIDER_STATUSES } from '../domain.js'
import { Card, EmptyState, ErrorMessage, SkeletonRows, StatusBadge } from './common.jsx'

export default function ProviderList({ filters, onFiltersChange, result, selectedId, onSelect }) {
  const { data: providers, error, loading, reload } = result
  const isFiltered = Boolean(filters.search || filters.status)

  return (
    <Card title="All providers" titleId="providers-heading" count={providers?.length}>
      <div className="toolbar">
        <label className="search-field">
          <span className="visually-hidden">Search providers</span>
          <input
            type="search"
            placeholder="Search by name or county"
            value={filters.search}
            maxLength={200}
            onChange={(event) => onFiltersChange({ ...filters, search: event.target.value })}
          />
        </label>
        <label>
          <span className="visually-hidden">Filter by status</span>
          <select value={filters.status} onChange={(event) => onFiltersChange({ ...filters, status: event.target.value })}>
            <option value="">All statuses</option>
            {PROVIDER_STATUSES.map((status) => (
              <option key={status}>{status}</option>
            ))}
          </select>
        </label>
      </div>

      <ErrorMessage error={error} onRetry={reload} />
      {loading && !providers && <SkeletonRows label="Loading providers" />}

      {providers?.length === 0 && (
        <EmptyState title={isFiltered ? 'No matching providers' : 'No providers yet'}>
          {isFiltered ? 'Try a different search or status.' : 'Create the first provider to get started.'}
        </EmptyState>
      )}

      {providers?.length > 0 && (
        <div className="table-wrap">
          <table className="table table-select">
            <thead>
              <tr>
                <th scope="col">Provider</th>
                <th scope="col" className="hide-sm">County</th>
                <th scope="col">Status</th>
                <th scope="col" className="num">
                  Licenses
                </th>
              </tr>
            </thead>
            <tbody aria-busy={loading}>
              {providers.map((provider) => (
                // The whole row is clickable; the button inside makes it reachable by keyboard
                // (its click bubbles up to the row, so there is one handler).
                <tr
                  key={provider.providerId}
                  className={provider.providerId === selectedId ? 'selected' : undefined}
                  onClick={() => onSelect(provider.providerId)}
                >
                  <td>
                    <button
                      type="button"
                      className="row-link"
                      aria-current={provider.providerId === selectedId ? 'true' : undefined}
                    >
                      {provider.providerName}
                    </button>
                  </td>
                  <td className="hide-sm">{provider.county}</td>
                  <td>
                    <StatusBadge status={provider.status} />
                  </td>
                  <td className="num">
                    {provider.licenseCount}
                    {provider.expiredLicenseCount > 0 && (
                      <span className="pill pill-red" title="Expired licenses">
                        {provider.expiredLicenseCount} expired
                      </span>
                    )}
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
