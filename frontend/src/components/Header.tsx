import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Header() {
  const { token, email, plan, analysesUsedThisMonth, analysesPerMonth } = useAuth()
  const { logout } = useAuth()
  const navigate = useNavigate()

  const remaining = analysesPerMonth === -1 ? null : analysesPerMonth - analysesUsedThisMonth
  const nearLimit = remaining !== null && remaining <= 5

  return (
    <header className="flex-shrink-0 h-14 flex items-center justify-between px-4 border-b border-gray-100 bg-white">
      {/* Logo */}
      <Link to="/" className="flex items-center gap-2 font-bold text-leaf-600 text-lg">
        🌿 <span>MonadicLeaf</span>
        <span className="text-xs font-normal text-gray-400 hidden sm:inline">Green C# AI</span>
      </Link>

      {/* Right side */}
      <div className="flex items-center gap-3">
        {/* Usage badge */}
        {token && remaining !== null && (
          <span className={`text-xs px-2 py-1 rounded-full font-mono ${
            nearLimit
              ? 'bg-amber-100 text-amber-700'
              : 'bg-gray-100 text-gray-500'
          }`}>
            {remaining}/{analysesPerMonth} left
            {nearLimit && ' ⚠'}
          </span>
        )}
        {token && analysesPerMonth === -1 && (
          <span className="text-xs px-2 py-1 rounded-full bg-leaf-100 text-leaf-700 font-mono">
            {plan} — unlimited
          </span>
        )}

        {/* Auth buttons */}
        {token ? (
          <div className="flex items-center gap-2">
            <span className="text-xs text-gray-500 hidden sm:inline">{email}</span>
            {plan === 'Free' && (
              <button
                onClick={() => navigate('/billing')}
                className="text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 text-white font-semibold transition-colors"
              >
                Go Pro €19
              </button>
            )}
            <button
              onClick={logout}
              className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 text-gray-600 hover:text-gray-900 transition-colors"
            >
              Sign out
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <Link
              to="/login"
              className="text-xs px-3 py-1.5 rounded-lg border border-gray-200 text-gray-600 hover:text-gray-900 transition-colors"
            >
              Sign in
            </Link>
            <Link
              to="/register"
              className="text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 text-white font-semibold transition-colors"
            >
              Go Pro €19
            </Link>
          </div>
        )}
      </div>
    </header>
  )
}
