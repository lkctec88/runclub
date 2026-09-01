import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function SuperAdminRoute() {
  const { isSuperAdmin, loading } = useAuth()
  if (loading) return <div className="page-loading">Loading…</div>
  if (!isSuperAdmin) return <Navigate to="/activities" replace />
  return <Outlet />
}
