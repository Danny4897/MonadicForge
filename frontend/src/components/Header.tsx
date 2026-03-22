import { useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Header() {
  const { token, email, plan, analysesUsedThisMonth, analysesPerMonth, logout } = useAuth()
  const navigate = useNavigate()

  const remaining = analysesPerMonth === -1 ? null : analysesPerMonth - analysesUsedThisMonth
  const nearLimit = remaining !== null && analysesPerMonth > 0 && remaining <= Math.ceil(analysesPerMonth * 0.1)

  const handleGoPro = useCallback(() => navigate('/billing'), [navigate])

  return (
    <header className="flex-shrink-0 h-14 flex items-center justify-between px-5 bg-white border-b border-gray-100 shadow-sm">
      {/* Logo */}
      <Link
        to="/"
        className="flex items-center gap-2.5 focus:outline-none focus:ring-2 focus:ring-leaf-500 rounded-lg"
      >
        <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-leaf-500 text-white text-base font-bold shadow-sm">
          🌿
        </div>
        <div className="flex flex-col leading-tight">
          <span className="font-bold text-gray-900 text-sm">MonadicLeaf</span>
          <span className="text-[10px] text-leaf-500 font-medium tracking-wide hidden sm:block">GREEN C# AI</span>
        </div>
      </Link>

      {/* Right side */}
      <div className="flex items-center gap-2.5">
        {token && remaining !== null && (
          <div className={`flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full font-mono border ${
            nearLimit
              ? 'bg-amber-50 text-amber-700 border-amber-200'
              : 'bg-gray-50 text-gray-500 border-gray-200'
          }`}>
            <span className={`w-1.5 h-1.5 rounded-full ${nearLimit ? 'bg-amber-400' : 'bg-leaf-400'}`} />
            {remaining}/{analysesPerMonth} left
          </div>
        )}
        {token && analysesPerMonth === -1 && (
          <div className="flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full font-mono bg-leaf-50 text-leaf-700 border border-leaf-200">
            <span className="w-1.5 h-1.5 rounded-full bg-leaf-500" />
            {plan}
          </div>
        )}

        {token ? (
          <div className="flex items-center gap-2">
            <span className="text-xs text-gray-400 hidden md:inline max-w-[180px] truncate">{email}</span>
            {plan === 'Free' && (
              <button
                onClick={handleGoPro}
                className="text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 text-white font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500 shadow-sm"
              >
                Go Pro €19
              </button>
            )}
            <button
              onClick={logout}
              className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 text-gray-500 hover:text-gray-900 hover:border-gray-300 transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500"
            >
              Sign out
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <Link
              to="/login"
              className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 text-gray-600 hover:text-gray-900 hover:border-gray-300 transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500"
            >
              Sign in
            </Link>
            <Link
              to="/register"
              className="text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 text-white font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500 shadow-sm"
            >
              Go Pro €19
            </Link>
          </div>
        )}
      </div>
    </header>
  )
}
