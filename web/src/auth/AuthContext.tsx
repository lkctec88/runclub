import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '../api/services'
import { getToken, setToken } from '../api/client'
import type { AuthUser, Club } from '../types'
import { ClubRole, PlatformRole } from '../types'
import { clubsApi } from '../api/services'

const CLUB_KEY = 'runclub_club'

interface AuthContextValue {
  user: AuthUser | null
  clubs: Club[]
  clubId: string | null
  setClubId: (id: string) => void
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (
    email: string,
    password: string,
    firstName: string,
    lastName: string,
    englandAthleticsNumber: string,
  ) => Promise<void>
  logout: () => void
  isSuperAdmin: boolean
  isClubAdmin: boolean
  refresh: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [clubs, setClubs] = useState<Club[]>([])
  const [clubId, setClubIdState] = useState<string | null>(localStorage.getItem(CLUB_KEY))
  const [loading, setLoading] = useState(true)

  const setClubId = useCallback((id: string) => {
    setClubIdState(id)
    localStorage.setItem(CLUB_KEY, id)
  }, [])

  const applySession = useCallback(async (token: string, authUser: AuthUser) => {
    setToken(token)
    setUser(authUser)
    const clubList = await clubsApi.list()
    setClubs(clubList)
    if (clubList.length > 0) {
      const saved = localStorage.getItem(CLUB_KEY)
      const valid = clubList.some((c) => c.id === saved)
      setClubId(valid && saved ? saved : clubList[0].id)
    }
  }, [setClubId])

  const refresh = useCallback(async () => {
    const token = getToken()
    if (!token) {
      setUser(null)
      setLoading(false)
      return
    }
    try {
      const res = await authApi.me()
      await applySession(res.token, res.user)
    } catch {
      setToken(null)
      setUser(null)
    } finally {
      setLoading(false)
    }
  }, [applySession])

  useEffect(() => {
    refresh()
  }, [refresh])

  const login = async (email: string, password: string) => {
    const res = await authApi.login(email, password)
    await applySession(res.token, res.user)
  }

  const register = async (
    email: string,
    password: string,
    firstName: string,
    lastName: string,
    englandAthleticsNumber: string,
  ) => {
    const res = await authApi.register({ firstName, lastName, email, password, englandAthleticsNumber })
    await applySession(res.token, res.user)
  }

  const logout = () => {
    setToken(null)
    setUser(null)
    setClubs([])
    setClubIdState(null)
    localStorage.removeItem(CLUB_KEY)
  }

  const isSuperAdmin =
    user?.platformRole === PlatformRole.SuperAdmin ||
    !!user?.memberships.some((m) => m.role === ClubRole.SuperAdmin)
  const isClubAdmin = useMemo(() => {
    if (isSuperAdmin) return true
    if (!clubId || !user) return false
    const m = user.memberships.find((x) => x.clubId === clubId)
    return m?.role === ClubRole.Admin || m?.role === ClubRole.SuperAdmin
  }, [isSuperAdmin, clubId, user])

  return (
    <AuthContext.Provider
      value={{
        user,
        clubs,
        clubId,
        setClubId,
        loading,
        login,
        register,
        logout,
        isSuperAdmin,
        isClubAdmin,
        refresh,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
