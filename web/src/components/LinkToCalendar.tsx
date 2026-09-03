import { useState } from 'react'
import { IconCalendarPlus } from '@tabler/icons-react'
import { profileApi } from '../api/services'
import { ApiError } from '../api/client'

export function LinkToCalendar() {
  const [url, setUrl] = useState('')
  const [copied, setCopied] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const createLink = async () => {
    setLoading(true)
    setError('')
    setCopied(false)
    try {
      const res = await profileApi.createFeedToken()
      setUrl(res.url)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create a calendar link')
    } finally {
      setLoading(false)
    }
  }

  const copy = async () => {
    if (!url) return
    await navigator.clipboard.writeText(url)
    setCopied(true)
  }

  const webcalUrl = url.replace(/^https?:\/\//, 'webcal://')

  return (
    <div className="card calendar-link-card">
      <button type="button" className="btn btn-outline" onClick={() => void createLink()} disabled={loading}>
        <IconCalendarPlus size={18} stroke={1.8} aria-hidden="true" />
        {loading ? 'Creating link…' : 'Link to my calendar'}
      </button>
      <p className="activity-meta" style={{ marginTop: '0.5rem' }}>
        Subscribe once. Your calendar app will keep pulling upcoming club activities — no .ics download.
      </p>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {url && (
        <div className="calendar-link-result">
          <input readOnly value={url} aria-label="Calendar subscribe URL" onFocus={(e) => e.target.select()} />
          <div className="admin-row-actions">
            <button type="button" className="btn btn-outline btn-sm" onClick={() => void copy()}>
              {copied ? 'Copied' : 'Copy link'}
            </button>
            <a className="btn btn-outline btn-sm" href={webcalUrl}>
              Open in calendar app
            </a>
          </div>
        </div>
      )}
    </div>
  )
}
