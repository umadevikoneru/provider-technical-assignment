import { lazy, Suspense, useState } from 'react'
import { SkeletonRows } from './components/common.jsx'
import DeletedProviders from './components/DeletedProviders.jsx'
import ProvidersPage from './components/ProvidersPage.jsx'
import ScenarioReports from './components/ScenarioReports.jsx'

// The dashboard pulls in the charting library; load it only when the tab is opened.
const Dashboard = lazy(() => import('./components/Dashboard.jsx'))

const TABS = [
  { id: 'providers', label: 'Providers', Component: ProvidersPage },
  { id: 'scenarios', label: 'License scenarios', Component: ScenarioReports },
  { id: 'dashboard', label: 'Dashboard', Component: Dashboard },
  { id: 'deleted', label: 'Deleted', Component: DeletedProviders },
]

function App() {
  const [activeTab, setActiveTab] = useState(TABS[0].id)
  const { Component } = TABS.find((tab) => tab.id === activeTab)

  return (
    <>
      <header className="app-header">
        <div className="app-header-inner">
          <div className="brand">
            <span className="brand-mark" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M12 3l7 3v5c0 4.5-3 8.3-7 10-4-1.7-7-5.5-7-10V6l7-3z" />
                <path d="M9 12l2 2 4-4" />
              </svg>
            </span>
            <span className="brand-name">Provider &amp; License Manager</span>
          </div>
          <nav className="tabs" aria-label="Sections">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                type="button"
                className={`tab${tab.id === activeTab ? ' tab-active' : ''}`}
                aria-current={tab.id === activeTab ? 'page' : undefined}
                onClick={() => setActiveTab(tab.id)}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>
      </header>
      <main className="app-main">
        <Suspense fallback={<SkeletonRows rows={6} />}>
          <Component />
        </Suspense>
      </main>
    </>
  )
}

export default App
