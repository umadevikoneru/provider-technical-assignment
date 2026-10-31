import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getDashboard } from '../api/reports.js'
import { statusTone } from '../domain.js'
import { useApi } from '../hooks/useApi.js'
import { describeDays, formatDate } from '../utils/format.js'
import { Card, EmptyState, ErrorMessage, PageHeader, SkeletonRows } from './common.jsx'

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
        description="Provider and license insights. Soft-deleted providers are excluded from every number except the audit count."
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
      <div className="stats">
        <Stat label="Providers" value={data.totalProviders} />
        <Stat label="Licenses" value={data.totalLicenses} />
        <Stat
          label="Active providers with expired licenses"
          value={data.activeProvidersWithExpiredLicenses}
          tone={data.activeProvidersWithExpiredLicenses > 0 ? 'red' : undefined}
        />
        <Stat
          label={`Licenses expiring within ${data.expiringWithinDays} days`}
          value={data.expiringSoon.length}
          tone={data.expiringSoon.length > 0 ? 'amber' : undefined}
        />
        <Stat
          label="Licenses with outdated recorded status"
          value={data.staleLicenseStatuses}
          tone={data.staleLicenseStatuses > 0 ? 'amber' : undefined}
        />
        <Stat label="Deleted providers (audit)" value={data.deletedProviders} />
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
                <Bar dataKey="count" name="Providers" radius={[6, 6, 0, 0]} maxBarSize={64}>
                  {data.providersByStatus.map((entry) => (
                    <Cell key={entry.status} fill={toneColor(entry.status)} />
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
                    <Pie data={licenseSlices} dataKey="count" nameKey="status" innerRadius="55%" outerRadius="85%" paddingAngle={2}>
                      {licenseSlices.map((entry) => (
                        <Cell key={entry.status} fill={toneColor(entry.status)} stroke="var(--surface)" />
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
        title={`Licenses expiring within ${data.expiringWithinDays} days`}
        titleId="expiring-heading"
        count={data.expiringSoon.length}
        description="Active licenses to renew soon, nearest first."
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

function Stat({ label, value, tone }) {
  return (
    <div className={`stat${tone ? ` stat-${tone}` : ''}`}>
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  )
}
