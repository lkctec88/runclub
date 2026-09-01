import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { clubLogoUrl } from '../branding'

const nav = [
  { to: '/activities', label: 'Activities', icon: '🏃' },
  { to: '/calendar', label: 'Calendar', icon: '📅' },
  { to: '/training', label: 'Train', icon: '🎯' },
  { to: '/community', label: 'Club', icon: '👥' },
  { to: '/profile', label: 'You', icon: '👤' },
]

export function Layout() {
  const { clubs, clubId, setClubId, logout, isClubAdmin, isSuperAdmin } = useAuth()
  const location = useLocation()
  const activeClub = clubs.find((c) => c.id === clubId)
  const inAdmin = location.pathname.startsWith('/admin') || location.pathname.startsWith('/superadmin')

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <img
            src={clubLogoUrl(activeClub?.logoUrl)}
            alt=""
            className={`brand-logo${activeClub?.logoUrl ? '' : ' brand-logo--default'}`}
          />
          <span className="brand-name">{activeClub?.name ?? 'RunClub'}</span>
        </div>
        {clubs.length > 1 && (
          <select
            className="club-select"
            value={clubId ?? ''}
            onChange={(e) => setClubId(e.target.value)}
            aria-label="Select club"
          >
            {clubs.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        )}
        <div className="topbar-right">
          {(isClubAdmin || isSuperAdmin) && (
            <div className="role-switch" role="group" aria-label="App section">
              <NavLink to="/activities" className={`role-switch-btn${!inAdmin ? ' active' : ''}`}>
                User
              </NavLink>
              <NavLink to="/admin" className={`role-switch-btn${inAdmin ? ' active' : ''}`}>
                Admin
              </NavLink>
            </div>
          )}
          <button type="button" className="btn btn-ghost btn-sm" onClick={logout}>
            Out
          </button>
        </div>
      </header>

      <main className="main-content">
        <Outlet />
      </main>

      <nav className="bottom-nav">
        {nav.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            <span>{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
