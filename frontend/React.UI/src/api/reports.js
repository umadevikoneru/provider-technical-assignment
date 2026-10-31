import { request } from './client.js'

export const getActiveProvidersActiveLicenses = (signal) =>
  request('/reports/active-providers-active-licenses', { signal })

export const getActiveProvidersExpiredLicenses = (signal) =>
  request('/reports/active-providers-expired-licenses', { signal })

export const getDashboard = (signal) => request('/dashboard', { signal })
