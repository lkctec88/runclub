import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { Layout } from './components/Layout'
import { LoginPage } from './pages/LoginPage'
import { ActivitiesPage } from './pages/ActivitiesPage'
import { ActivityDetailPage } from './pages/ActivityDetailPage'
import { CalendarPage } from './pages/CalendarPage'
import { TrainingPage } from './pages/TrainingPage'
import { CommunityPage } from './pages/CommunityPage'
import { VolunteerPage } from './pages/VolunteerPage'
import { ProfilePage } from './pages/ProfilePage'
import { AdminPage } from './pages/AdminPage'
import { AdminRoute } from './auth/AdminRoute'

function LegacyRunRedirect() {
  const { id } = useParams<{ id: string }>()
  return <Navigate to={id ? `/activities/${id}` : '/activities'} replace />
}

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route index element={<Navigate to="/activities" replace />} />
            <Route path="activities" element={<ActivitiesPage />} />
            <Route path="activities/:id" element={<ActivityDetailPage />} />
            <Route path="runs" element={<Navigate to="/activities" replace />} />
            <Route path="runs/:id" element={<LegacyRunRedirect />} />
            <Route path="calendar" element={<CalendarPage />} />
            <Route path="training" element={<TrainingPage />} />
            <Route path="community" element={<CommunityPage />} />
            <Route path="volunteer" element={<VolunteerPage />} />
            <Route path="profile" element={<ProfilePage />} />
            <Route element={<AdminRoute />}>
              <Route path="admin" element={<AdminPage />} />
              <Route path="superadmin" element={<Navigate to="/admin" replace />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  )
}

export default App
