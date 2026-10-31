import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getDashboard } from '../api/reports.js'
import { statusTone } from '../domain.js'
import { useApi } from '../hooks/useApi.js'
import { describeDays, formatDate, pluralize } from '../utils/format.js'
import { Card, EmptyState, ErrorMessage, PageHeader, SkeletonRows } from './common.jsx'

// Charts use the same status colors as the dots everywhere else (see index.css).
const toneColor = (status) => `var(--${statusTone(status)})`

const tooltipStyle = {
  background: 'var(--surface)',
  border: '1px solid var(--border)',
  borderRadius: 8,
  color: 'var(--text)',
}

/** Read-only insights. Every number is computed by the API from the database views. */
export default function Dashboard() {
  const { data, error, loading, reload } = useApi(getDashboard)

  return (
    <>
      <PageHeader
        title="Dashboard"
        description="Key provider and license figures. Deleted providers are excluded."
      />
      <ErrorMessage error={error} onRetry={reload} />
      {loading && !data && <SkeletonRows rows={6} label="Loading dashboard" />}
      {data && <DashboardContent data={data} />}
    </>
  )
}

function DashboardContent({ data }) {
  const licenseSlices = data.licensesByEffectiveStatus.filter((entry) => entry.count > 0)

  return (
    <div className="stack">
      <div>
        <div className="stats">
          <Stat label="Providers" value={data.totalProviders} />
          <Stat label="Licenses" value={data.totalLicenses} />
          <Stat
            label="Active providers with expired licenses"
            value={data.activeProvidersWithExpiredLicenses}
            tone={data.activeProvidersWithExpiredLicenses > 0 ? 'red' : undefined}
          />
          <Stat
            label={`Expiring within ${data.expiringWithinDays} days`}
            value={data.expiringSoon.length}
            tone={data.expiringSoon.length > 0 ? 'amber' : undefined}
          />
        </div>
        {/* Secondary figures stay visible without competing with the four above. */}
        <p className="stat-note">
          {pluralize(data.staleLicenseStatuses, 'license')} still recorded as Active after expiring ·{' '}
          {pluralize(data.deletedProviders, 'deleted provider')} kept for audit
        </p>
      </div>

      <div className="split-even">
        <Card title="Providers by status" titleId="by-status-heading">
          <div className="chart">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data.providersByStatus} margin={{ top: 8, right: 8, bottom: 0, left: -20 }}>
                <CartesianGrid vertical={false} stroke="var(--border)" />
                <XAxis dataKey="status" tick={{ fill: 'var(--text-muted)' }} axisLine={false} tickLine={false} />
                <YAxis allowDecimals={false} tick={{ fill: 'var(--text-muted)' }} axisLine={false} tickLine={false} />
                <Tooltip cursor={{ fill: 'var(--surface-hover)' }} contentStyle={tooltipStyle} />
                <Bar dataKey="count" name="Providers" radius={[4, 4, 0, 0]} maxBarSize={44}>
                  {data.providersByStatus.map((entry) => (
                    <Cell key={entry.status} fill={toneColor(entry.status)} fillOpacity={0.85} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card title="Licenses by effective status" titleId="licenses-heading">
          {licenseSlices.length === 0 ? (
            <EmptyState title="No licenses" />
          ) : (
            <>
              <div className="chart">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={licenseSlices} dataKey="count" nameKey="status" innerRadius="62%" outerRadius="86%" paddingAngle={2} strokeWidth={2}>
                      {licenseSlices.map((entry) => (
                        <Cell key={entry.status} fill={toneColor(entry.status)} fillOpacity={0.85} stroke="var(--surface)" />
                      ))}
                    </Pie>
                    <Tooltip contentStyle={tooltipStyle} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
              <ul className="legend">
                {licenseSlices.map((entry) => (
                  <li key={entry.status}>
                    <span className="legend-swatch" style={{ background: toneColor(entry.status) }} aria-hidden="true" />
                    {entry.status} <strong>{entry.count}</strong>
                  </li>
                ))}
              </ul>
            </>
          )}
        </Card>
      </div>

      <Card
        title="Upcoming renewals"
        titleId="expiring-heading"
        count={data.expiringSoon.length}
        description={`Active licenses expiring within ${data.expiringWithinDays} days, nearest first.`}
      >
        {data.expiringSoon.length === 0 ? (
          <EmptyState title="Nothing expiring soon" />
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Provider</th>
                  <th scope="col">License</th>
                  <th scope="col">Expires</th>
                </tr>
              </thead>
              <tbody>
                {data.expiringSoon.map((license) => (
                  <tr key={`${license.providerId}-${license.licenseNumber}`}>
                    <td>{license.providerName}</td>
                    <td className="mono">{license.licenseNumber}</td>
                    <td>
                      {formatDate(license.expirationDate)}
                      <div className="muted small">{describeDays(license.daysUntilExpiration)}</div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  )
}

/** A single number. `tone` adds a small status dot when the value needs attention. */
function Stat({ label, value, tone }) {
  return (
    <div className="stat">
      <div className="stat-label">
        {tone && <span className={`stat-indicator stat-indicator-${tone}`} aria-hidden="true" />}
        {label}
      </div>
      <div className="stat-value">{value}</div>
    </div>
  )
}
