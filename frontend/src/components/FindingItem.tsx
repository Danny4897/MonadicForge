import { useState } from 'react'
import type { Finding } from '../types/api'

interface Props {
  finding: Finding
  onNavigate: (line: number, column: number) => void
}

const SEV_STYLES: Record<string, { card: string; dot: string; badge: string }> = {
  Error:   { card: 'border-red-200 bg-red-50/60',   dot: 'bg-red-500',   badge: 'bg-red-100 text-red-700' },
  Warning: { card: 'border-amber-200 bg-amber-50/60', dot: 'bg-amber-400', badge: 'bg-amber-100 text-amber-700' },
  Info:    { card: 'border-blue-200 bg-blue-50/60',  dot: 'bg-blue-400',  badge: 'bg-blue-100 text-blue-700' },
}

const DEFAULT_STYLE = { card: 'border-gray-200 bg-gray-50', dot: 'bg-gray-400', badge: 'bg-gray-100 text-gray-600' }

export default function FindingItem({ finding, onNavigate }: Props) {
  const [expanded, setExpanded] = useState(false)
  const style = SEV_STYLES[finding.severity] ?? DEFAULT_STYLE

  return (
    <div className={`rounded-xl border text-sm overflow-hidden ${style.card}`}>
      {/* Main row */}
      <div className="flex items-start gap-2.5 p-3">
        <span className={`mt-1.5 flex-shrink-0 h-2 w-2 rounded-full ${style.dot}`} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap mb-0.5">
            <span className={`text-[10px] font-mono font-semibold px-1.5 py-0.5 rounded ${style.badge}`}>
              {finding.ruleId}
            </span>
            <button
              onClick={() => onNavigate(finding.line, finding.column)}
              className="text-[10px] text-gray-400 hover:text-gray-700 font-mono transition-colors focus:outline-none focus:ring-1 focus:ring-leaf-500 rounded"
            >
              :{finding.line}
            </button>
          </div>
          <p className="font-semibold text-gray-800 text-xs leading-snug">{finding.title}</p>
          <p className="text-gray-500 text-xs mt-0.5 leading-relaxed">{finding.message}</p>
          {finding.llmExplanation && (
            <p className="text-xs text-gray-400 italic mt-1 leading-relaxed">{finding.llmExplanation}</p>
          )}
        </div>
        {finding.llmSuggestedFix && (
          <button
            onClick={() => setExpanded(e => !e)}
            className={`flex-shrink-0 text-[10px] font-medium px-2 py-1 rounded-lg transition-colors focus:outline-none focus:ring-1 focus:ring-leaf-500 ${
              expanded ? 'bg-gray-200 text-gray-700' : 'bg-white/70 text-gray-500 hover:text-gray-700'
            }`}
          >
            {expanded ? '▲' : '▼ Fix'}
          </button>
        )}
      </div>

      {/* Expanded fix */}
      {expanded && finding.llmSuggestedFix && (
        <div className="border-t border-current/10 bg-white/50 px-3 py-2">
          <p className="text-[10px] font-semibold uppercase tracking-wide text-gray-400 mb-1.5">Suggested fix</p>
          <pre className="text-xs font-mono whitespace-pre-wrap overflow-x-auto text-gray-700 leading-relaxed">
            {finding.llmSuggestedFix}
          </pre>
        </div>
      )}
    </div>
  )
}
