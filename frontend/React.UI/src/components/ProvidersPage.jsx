import { useCallback, useEffect, useRef, useState } from 'react'
import { listProviders } from '../api/providers.js'
import { useApi } from '../hooks/useApi.js'
import { useDebouncedValue } from '../hooks/useDebouncedValue.js'
import { Card, EmptyState, Notice, PageHeader } from './common.jsx'
import ProviderDetail from './ProviderDetail.jsx'
import ProviderForm from './ProviderForm.jsx'
import ProviderList from './ProviderList.jsx'

/**
 * Master/detail screen. The right-hand panel shows one of:
 *   null                       nothing selected
 *   { mode: 'detail', id }     a provider and its licenses
 *   { mode: 'create' }         the new-provider form
 *   { mode: 'edit', provider } the edit form
 */
export default function ProvidersPage() {
  const [filters, setFilters] = useState({ search: '', status: '' })
  const search = useDebouncedValue(filters.search.trim(), 300)
  const loadProviders = useCallback(
    (signal) => listProviders({ search, status: filters.status }, signal),
    [search, filters.status],
  )
  const providers = useApi(loadProviders)

  const [panel, setPanel] = useState(null)
  const [notice, setNotice] = useState(null)
  const dismissNotice = useCallback(() => setNotice(null), [])
  const panelRef = useRef(null)

  function openPanel(next, message = null) {
    setPanel(next)
    setNotice(message)
  }

  function handleSaved(saved, verb) {
    providers.reload()
    openPanel({ mode: 'detail', id: saved.providerId }, `${saved.providerName} was ${verb}.`)
  }

  function handleDeleted(provider) {
    providers.reload()
    openPanel(null, `${provider.providerName} was deleted. You can restore it from the Deleted tab.`)
  }

  // On narrow screens the panel is stacked above the list (see .panel-column in index.css);
  // scroll up to it when it changes so the user sees what they just opened or saved.
  useEffect(() => {
    if ((panel || notice) && window.matchMedia('(max-width: 900px)').matches) {
      panelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }, [panel, notice])

  const selectedId = panel?.mode === 'detail' ? panel.id : panel?.mode === 'edit' ? panel.provider.providerId : null

  return (
    <>
      <PageHeader
        title="Providers"
        description="Search, review and maintain providers. Deleted providers are hidden here and kept for audit."
        actions={
          <button type="button" className="button-primary" onClick={() => openPanel({ mode: 'create' })}>
            + New provider
          </button>
        }
      />

      <div className="split">
        <ProviderList
          filters={filters}
          onFiltersChange={setFilters}
          result={providers}
          selectedId={selectedId}
          onSelect={(id) => openPanel({ mode: 'detail', id })}
        />

        <div className="panel-column" ref={panelRef}>
          <Notice message={notice} onDismiss={dismissNotice} />

          {panel === null && (
            <div className="panel-placeholder">
              <Card>
                <EmptyState title="No provider selected">
                  Select a provider to see its licenses and history, or create a new one.
                </EmptyState>
              </Card>
            </div>
          )}

          {panel?.mode === 'detail' && (
            <ProviderDetail
              key={panel.id}
              providerId={panel.id}
              onEdit={(provider) => openPanel({ mode: 'edit', provider })}
              onDeleted={handleDeleted}
            />
          )}

          {panel?.mode === 'create' && (
            <ProviderForm onSaved={(saved) => handleSaved(saved, 'created')} onCancel={() => openPanel(null)} />
          )}

          {panel?.mode === 'edit' && (
            <ProviderForm
              key={panel.provider.providerId}
              provider={panel.provider}
              onSaved={(saved) => handleSaved(saved, 'updated')}
              onCancel={() => openPanel({ mode: 'detail', id: panel.provider.providerId })}
            />
          )}
        </div>
      </div>
    </>
  )
}
