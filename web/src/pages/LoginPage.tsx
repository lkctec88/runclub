import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { IconEye, IconEyeOff } from '@tabler/icons-react'
import { useAuth } from '../auth/AuthContext'
import { ApiError } from '../api/client'
import { DEFAULT_CLUB_LOGO } from '../branding'

export function LoginPage() {
  const { user, login, register } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [eaNumber, setEaNumber] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (user) return <Navigate to="/activities" replace />

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      if (mode === 'login') await login(email, password)
      else await register(email, password, firstName, lastName, eaNumber)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <img src={DEFAULT_CLUB_LOGO} alt="Activity Club" className="login-logo login-logo--default" />
        <p>
          {mode === 'register'
            ? 'Register with the last name and England Athletics number your club has on file.'
            : 'Sign in to view activities, volunteer, and train with your club'}
        </p>

        <form onSubmit={submit}>
          {mode === 'register' && (
            <>
              <div className="form-group">
                <label htmlFor="firstName">First name</label>
                <input id="firstName" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </div>
              <div className="form-group">
                <label htmlFor="lastName">Last name</label>
                <input id="lastName" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </div>
              <div className="form-group">
                <label htmlFor="eaNumber">England Athletics number</label>
                <input
                  id="eaNumber"
                  value={eaNumber}
                  onChange={(e) => setEaNumber(e.target.value)}
                  required
                  autoComplete="off"
                />
              </div>
            </>
          )}
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <div className="password-field">
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                minLength={8}
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword((open) => !open)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                aria-pressed={showPassword}
              >
                {showPassword ? (
                  <IconEyeOff size={20} stroke={1.8} aria-hidden="true" />
                ) : (
                  <IconEye size={20} stroke={1.8} aria-hidden="true" />
                )}
              </button>
            </div>
          </div>
          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Please wait…' : mode === 'login' ? 'Sign in' : 'Create account'}
          </button>
        </form>

        <p style={{ marginTop: '1rem', fontSize: '0.85rem' }}>
          {mode === 'login' ? (
            <>
              New here?{' '}
              <button type="button" className="btn btn-ghost btn-sm" style={{ color: 'var(--navy)' }} onClick={() => setMode('register')}>
                Register
              </button>
            </>
          ) : (
            <>
              Already have an account?{' '}
              <button type="button" className="btn btn-ghost btn-sm" style={{ color: 'var(--navy)' }} onClick={() => setMode('login')}>
                Sign in
              </button>
            </>
          )}
        </p>
      </div>
    </div>
  )
}
