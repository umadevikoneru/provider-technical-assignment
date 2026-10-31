import { useEffect } from 'react'
import { statusTone } from '../domain.js'

// Small presentational building blocks shared by every screen.

export function PageHeader({ title, description, actions }) {
  return (
    <div className="page-header">
      <div>
        <h1>{title}</h1>
        {description && <p className="page-description">{description}</p>}
      </div>
      {actions && <div className="actions">{actions}</div>}
    </div>
  )
}

export function Card({ title, titleId, count, description, actions, children }) {
  return (
    <section className="card" aria-labelledby={title ? titleId : undefined}>
      {(title || actions) && (
        <header className="card-header">
          <div>
            {title && (
              <h2 id={titleId}>
                {title}
                {count !== undefined && <span className="count">{count}</span>}
              </h2>
            )}
            {description && <p className="muted small">{description}</p>}
          </div>
          {actions && <div className="actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  )
}

export function StatusBadge({ status }) {
  return (
    <span className={`badge badge-${statusTone(status)}`}>
      <span className="badge-dot" aria-hidden="true" />
      {status}
    </span>
  )
}

/** Success message that dismisses itself after a few seconds. */
export function Notice({ message, onDismiss }) {
  useEffect(() => {
    if (!message) return undefined
    const timer = setTimeout(onDismiss, 6000)
    return () => clearTimeout(timer)
  }, [message, onDismiss])

  if (!message) return null
  return (
    <div className="alert alert-success" role="status">
      <span>{message}</span>
      <button type="button" className="icon-button" aria-label="Dismiss message" onClick={onDismiss}>
        ×
      </button>
    </div>
  )
}

export function ErrorMessage({ error, onRetry }) {
  if (!error) return null
  return (
    <div className="alert alert-error" role="alert">
      <span>{error.message}</span>
      {onRetry && (
        <button type="button" className="button-link" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function EmptyState({ title, children }) {
  return (
    <div className="empty-state">
      <p className="empty-title">{title}</p>
      {children && <p className="muted small">{children}</p>}
    </div>
  )
}

/** Placeholder rows shown while a table loads for the first time. */
export function SkeletonRows({ rows = 5, label = 'Loading' }) {
  return (
    <div className="skeleton-list" role="status" aria-label={label}>
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="skeleton-row" />
      ))}
    </div>
  )
}
