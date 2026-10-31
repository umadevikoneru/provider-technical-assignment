// Values the API accepts and returns. The API validates them; the UI only uses them for inputs and colors.

export const PROVIDER_STATUSES = ['Active', 'Inactive', 'Pending']

// Same meaning everywhere (badges, charts): green = good, amber = attention, red = problem, gray = dormant.
const STATUS_TONE = {
  Active: 'green',
  Pending: 'amber',
  Suspended: 'amber',
  Inactive: 'gray',
  Expired: 'red',
}

export const statusTone = (status) => STATUS_TONE[status] ?? 'gray'
