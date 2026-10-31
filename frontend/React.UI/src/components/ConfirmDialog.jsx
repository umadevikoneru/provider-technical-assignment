import { useEffect, useRef } from 'react'

/** Modal confirmation built on the native <dialog> element (focus trap, Esc and backdrop for free). */
export default function ConfirmDialog({
  title,
  children,
  confirmLabel,
  busyLabel = 'Working…',
  busy,
  error,
  onConfirm,
  onCancel,
}) {
  const dialogRef = useRef(null)

  useEffect(() => {
    const dialog = dialogRef.current
    dialog.showModal()
    return () => dialog.close()
  }, [])

  return (
    <dialog
      ref={dialogRef}
      className="dialog"
      aria-labelledby="confirm-title"
      onCancel={(event) => {
        // Esc: keep React in control of whether the dialog is open.
        event.preventDefault()
        if (!busy) onCancel()
      }}
    >
      <h2 id="confirm-title">{title}</h2>
      <div className="dialog-body">{children}</div>
      {error && (
        <div className="alert alert-error" role="alert">
          <span>{error.message}</span>
        </div>
      )}
      <div className="actions">
        <button type="button" onClick={onCancel} disabled={busy} autoFocus>
          Cancel
        </button>
        <button type="button" className="button-danger-solid" onClick={onConfirm} disabled={busy}>
          {busy ? busyLabel : confirmLabel}
        </button>
      </div>
    </dialog>
  )
}
