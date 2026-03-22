import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import type { AuthResponse } from '../types/api'

interface AuthState {
  token: string | null
  email: string | null
  plan: string | null
  analysesUsedThisMonth: number
  analysesPerMonth: number
}

interface AuthContextValue extends AuthState {
  login: (res: AuthResponse) => void
  logout: () => void
  refreshUsage: (used: number) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function loadInitialState(): AuthState {
  const token = localStorage.getItem('leaf_token')
  const email = localStorage.getItem('leaf_email')
  const plan = localStorage.getItem('leaf_plan')
  const used = parseInt(localStorage.getItem('leaf_used') ?? '0', 10)
  const perMonth = parseInt(localStorage.getItem('leaf_per_month') ?? '50', 10)
  return { token, email, plan, analysesUsedThisMonth: used, analysesPerMonth: perMonth }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadInitialState)

  const login = useCallback((res: AuthResponse) => {
    localStorage.setItem('leaf_token', res.token)
    localStorage.setItem('leaf_email', res.email)
    localStorage.setItem('leaf_plan', res.plan)
    localStorage.setItem('leaf_used', String(res.analysesUsedThisMonth))
    localStorage.setItem('leaf_per_month', String(res.analysesPerMonth))
    setState({
      token: res.token,
      email: res.email,
      plan: res.plan,
      analysesUsedThisMonth: res.analysesUsedThisMonth,
      analysesPerMonth: res.analysesPerMonth,
    })
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('leaf_token')
    localStorage.removeItem('leaf_email')
    localStorage.removeItem('leaf_plan')
    localStorage.removeItem('leaf_used')
    localStorage.removeItem('leaf_per_month')
    setState({ token: null, email: null, plan: null, analysesUsedThisMonth: 0, analysesPerMonth: 50 })
  }, [])

  const refreshUsage = useCallback((used: number) => {
    localStorage.setItem('leaf_used', String(used))
    setState(s => ({ ...s, analysesUsedThisMonth: used }))
  }, [])

  return (
    <AuthContext.Provider value={{ ...state, login, logout, refreshUsage }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
