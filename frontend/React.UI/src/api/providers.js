import { request } from './client.js'

export function listProviders({ search, status } = {}, signal) {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  if (status) params.set('status', status)
  const query = params.toString()
  return request(`/providers${query ? `?${query}` : ''}`, { signal })
}

export const getProvider = (id, signal) => request(`/providers/${id}`, { signal })

export const createProvider = (provider) => request('/providers', { method: 'POST', body: provider })

export const updateProvider = (id, provider) => request(`/providers/${id}`, { method: 'PUT', body: provider })

// Soft delete: the API flags the record; nothing is removed from the database.
export const deleteProvider = (id) => request(`/providers/${id}`, { method: 'DELETE' })

export const listDeletedProviders = (signal) => request('/providers/deleted', { signal })

export const restoreProvider = (id) => request(`/providers/${id}/restore`, { method: 'POST' })

export const getProviderAudit = (id, signal) => request(`/providers/${id}/audit`, { signal })
