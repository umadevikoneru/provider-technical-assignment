import { cloneElement, useId, useState } from 'react'
import { ApiError } from '../api/client.js'
import { createProvider, updateProvider } from '../api/providers.js'
import { PROVIDER_STATUSES } from '../domain.js'
import { Card } from './common.jsx'

const EMPTY = { providerName: '', county: '', status: 'Pending' }

// Quick checks for immediate feedback only. The API validates again and is the authority.
function validate(values) {
  const errors = {}
  const name = values.providerName.trim()
  const county = values.county.trim()

  if (!name) errors.providerName = ['Provider name is required.']
  else if (name.length > 200) errors.providerName = ['Provider name must be 200 characters or fewer.']

  if (!county) errors.county = ['County is required.']
  else if (county.length > 100) errors.county = ['County must be 100 characters or fewer.']

  return errors
}

/** Create form when `provider` is omitted, edit form otherwise. */
export default function ProviderForm({ provider, onSaved, onCancel }) {
  const isEdit = Boolean(provider)
  const [values, setValues] = useState(
    isEdit ? { providerName: provider.providerName, county: provider.county, status: provider.status } : EMPTY,
  )
  const [fieldErrors, setFieldErrors] = useState({})
  const [formError, setFormError] = useState(null)
  const [saving, setSaving] = useState(false)

  const setField = (name) => (event) => setValues({ ...values, [name]: event.target.value })

  async function handleSubmit(event) {
    event.preventDefault()
    setFormError(null)

    const localErrors = validate(values)
    setFieldErrors(localErrors)
    if (Object.keys(localErrors).length > 0) return

    setSaving(true)
    try {
      const saved = isEdit
        ? await updateProvider(provider.providerId, { ...values, version: provider.version })
        : await createProvider(values)
      onSaved(saved)
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors)
      } else {
        setFormError(error)
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card
      title={isEdit ? 'Edit provider' : 'New provider'}
      titleId="form-heading"
      description={isEdit ? provider.providerName : 'Licenses are managed separately and can be added later.'}
    >
      <form onSubmit={handleSubmit} noValidate>
        <Field label="Provider name" name="providerName" errors={fieldErrors.providerName}>
          <input value={values.providerName} onChange={setField('providerName')} maxLength={200} autoFocus />
        </Field>
        <Field label="County" name="county" errors={fieldErrors.county}>
          <input value={values.county} onChange={setField('county')} maxLength={100} placeholder="e.g. Fulton" />
        </Field>
        <Field label="Status" name="status" errors={fieldErrors.status}>
          <select value={values.status} onChange={setField('status')}>
            {PROVIDER_STATUSES.map((status) => (
              <option key={status}>{status}</option>
            ))}
          </select>
        </Field>

        {/* The API's message says what to do, e.g. "Reload it and try again" for a stale version. */}
        {formError && (
          <div className="alert alert-error" role="alert">
            <span>{formError.message}</span>
          </div>
        )}

        <div className="actions form-actions">
          <button type="button" onClick={onCancel} disabled={saving}>
            Cancel
          </button>
          <button type="submit" className="button-primary" disabled={saving}>
            {saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create provider'}
          </button>
        </div>
      </form>
    </Card>
  )
}

function Field({ label, name, errors, children }) {
  const id = useId()
  const errorId = `${id}-error`
  const hasError = Boolean(errors?.length)

  return (
    <div className={`field${hasError ? ' field-invalid' : ''}`}>
      <label htmlFor={id}>{label}</label>
      {/* Wire the id and error description onto the single input/select child. */}
      {cloneElement(children, {
        id,
        name,
        'aria-invalid': hasError || undefined,
        'aria-describedby': hasError ? errorId : undefined,
      })}
      {hasError && (
        <p id={errorId} className="field-error">
          {errors.join(' ')}
        </p>
      )}
    </div>
  )
}
