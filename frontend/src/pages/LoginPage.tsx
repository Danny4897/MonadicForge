import { useState, useCallback, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'

function extractMessage(err: unknown, fallback: string): string {
  if (err && typeof err === 'object' && 'message' in err && typeof err.message === 'string') {
    return err.message
  }
  return fallback
}

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = useCallback(async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await api.auth.login(email, password)
      login(res)
      navigate('/')
    } catch (err) {
      setError(extractMessage(err, 'Sign in failed. Please check your credentials.'))
    } finally {
      setLoading(false)
    }
  }, [email, password, login, navigate])

  return (
    <div className="flex flex-1 overflow-hidden">
      {/* ── Left brand panel ── */}
      <div className="hidden lg:flex flex-col justify-between w-[420px] flex-shrink-0 bg-gradient-to-br from-leaf-600 via-leaf-500 to-leaf-400 p-10 text-white">
        <div>
          <div className="flex items-center gap-2.5 mb-12">
            <div className="w-9 h-9 rounded-xl bg-white/20 flex items-center justify-center text-xl backdrop-blur-sm">🌿</div>
            <span className="font-bold text-lg">MonadicLeaf</span>
          </div>
          <h2 className="text-3xl font-bold leading-tight mb-4">
            Write greener C# code, automatically.
          </h2>
          <p className="text-leaf-100 text-sm leading-relaxed">
            MonadicLeaf analyzes your C# code against MonadicSharp green-code rules and suggests AI-powered fixes — right in your browser.
          </p>
        </div>

        <div className="space-y-4">
          {[
            { rule: 'GC001', text: 'Never try/catch inside Bind' },
            { rule: 'GC002', text: 'Map only for infallible transforms' },
            { rule: 'GC003', text: 'Validate before expensive I/O' },
          ].map(({ rule, text }) => (
            <div key={rule} className="flex items-center gap-3 bg-white/10 rounded-xl px-4 py-3 backdrop-blur-sm">
              <span className="font-mono text-xs bg-white/20 px-2 py-1 rounded-lg font-bold">{rule}</span>
              <span className="text-sm text-leaf-50">{text}</span>
            </div>
          ))}
        </div>
      </div>

      {/* ── Right form panel ── */}
      <div className="flex flex-1 items-center justify-center bg-gray-50 px-4">
        <div className="w-full max-w-sm">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-8 justify-center">
            <div className="w-8 h-8 rounded-lg bg-leaf-500 flex items-center justify-center text-white">🌿</div>
            <span className="font-bold text-gray-900">MonadicLeaf</span>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
            <div className="mb-6">
              <h1 className="text-xl font-bold text-gray-900">Welcome back</h1>
              <p className="text-sm text-gray-500 mt-1">Sign in to your account</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5" htmlFor="email">
                  Email
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  disabled={loading}
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-leaf-500 focus:border-transparent disabled:opacity-50 transition-shadow"
                  placeholder="you@example.com"
                  autoComplete="email"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5" htmlFor="password">
                  Password
                </label>
                <input
                  id="password"
                  type="password"
                  required
                  disabled={loading}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-leaf-500 focus:border-transparent disabled:opacity-50 transition-shadow"
                  placeholder="••••••••"
                  autoComplete="current-password"
                />
              </div>

              {error && (
                <div className="flex items-start gap-2 text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl px-3 py-2.5" role="alert">
                  <span className="mt-0.5">⚠</span>
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                aria-disabled={loading}
                className="w-full rounded-xl bg-leaf-500 hover:bg-leaf-600 disabled:opacity-50 text-white font-semibold text-sm py-2.5 transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500 shadow-sm mt-2"
              >
                {loading ? (
                  <span className="flex items-center justify-center gap-2">
                    <span className="h-4 w-4 rounded-full border-2 border-white/30 border-t-white animate-spin" />
                    Signing in…
                  </span>
                ) : 'Sign in'}
              </button>
            </form>

            <p className="mt-5 text-center text-sm text-gray-500">
              No account?{' '}
              <Link to="/register" className="text-leaf-600 font-semibold hover:underline focus:outline-none focus:ring-2 focus:ring-leaf-500 rounded">
                Create one free
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
