// Display formatting only. Values such as "days until expiration" are computed by the API.

/** "2026-11-19" (a calendar date, no time zone) -> local date text. */
export function formatDate(isoDate) {
  if (!isoDate) return '—'
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { dateStyle: 'medium' })
}

/** UTC instant from the API -> local date and time text. */
export function formatDateTime(isoInstant) {
  if (!isoInstant) return '—'
  return new Date(isoInstant).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

export function pluralize(count, singular, plural = `${singular}s`) {
  return `${count} ${count === 1 ? singular : plural}`
}

/** 20 -> "in 20 days", 0 -> "today", -5 -> "5 days ago". */
export function describeDays(days) {
  if (days === 0) return 'today'
  return days > 0 ? `in ${pluralize(days, 'day')}` : `${pluralize(-days, 'day')} ago`
}
