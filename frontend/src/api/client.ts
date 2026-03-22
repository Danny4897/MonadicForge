import type { AnalyzeResult, AnalysisRecord, AuthResponse, MeResponse, ApiError } from '../types/api'

const BASE = import.meta.env.VITE_API_URL ?? ''

async function request<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const token = localStorage.getItem('leaf_token')
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, { ...options, headers })

  if (!res.ok) {
    let err: ApiError = { code: 'UNKNOWN', message: res.statusText }
    try { err = await res.json() } catch { /* non-JSON error */ }
    throw err
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  auth: {
    register: (email: string, password: string) =>
      request<AuthResponse>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      }),
    login: (email: string, password: string) =>
      request<AuthResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      }),
    me: () => request<MeResponse>('/api/auth/me'),
  },
  analyze: {
    run: (code: string, fileName?: string) =>
      request<AnalyzeResult>('/api/analyze', {
        method: 'POST',
        body: JSON.stringify({ code, fileName }),
      }),
    history: (page = 1, pageSize = 20) =>
      request<AnalysisRecord[]>(`/api/analyze/history?page=${page}&pageSize=${pageSize}`),
  },
}
