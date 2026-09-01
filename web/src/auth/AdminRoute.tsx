import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function AdminRoute() {
  const { isClubAdmin, loading } = useAuth()
  if (loading) return <div className="page-loading">Loading…</div>
  if (!isClubAdmin) return <Navigate to="/activities" replace />
  return <Outlet />
}
