import { useNavigate } from 'react-router-dom'

interface Props {
  onClose: () => void
}

export default function UpgradeModal({ onClose }: Props) {
  const navigate = useNavigate()

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 p-6">
        <div className="text-center">
          <div className="text-4xl mb-2">🌿</div>
          <h2 className="text-xl font-bold text-gray-900">Monthly limit reached</h2>
          <p className="mt-2 text-sm text-gray-500">
            You've used all your free analyses for this month. Upgrade to Pro for unlimited analyses.
          </p>
        </div>

        <div className="mt-6 rounded-xl border-2 border-leaf-500 p-4">
          <div className="flex items-baseline gap-1">
            <span className="text-3xl font-bold text-leaf-600">€19</span>
            <span className="text-gray-500">/month</span>
          </div>
          <ul className="mt-3 space-y-1 text-sm text-gray-700">
            <li className="flex items-center gap-2">
              <span className="text-leaf-500">✓</span> Unlimited analyses
            </li>
            <li className="flex items-center gap-2">
              <span className="text-leaf-500">✓</span> AI explanations & fixes
            </li>
            <li className="flex items-center gap-2">
              <span className="text-leaf-500">✓</span> Code generation (coming soon)
            </li>
            <li className="flex items-center gap-2">
              <span className="text-leaf-500">✓</span> GitHub PR review (coming soon)
            </li>
          </ul>
        </div>

        <div className="mt-4 flex flex-col gap-2">
          <button
            onClick={() => navigate('/billing')}
            className="w-full rounded-xl bg-leaf-500 hover:bg-leaf-600 text-white font-semibold py-2.5 transition-colors"
          >
            Go Pro — €19/mo
          </button>
          <button
            onClick={onClose}
            className="w-full rounded-xl border border-gray-200 text-gray-500 hover:text-gray-700 py-2.5 text-sm transition-colors"
          >
            Maybe later
          </button>
        </div>
      </div>
    </div>
  )
}
