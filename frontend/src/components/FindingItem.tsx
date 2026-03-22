import { useState } from 'react'
import type { Finding } from '../types/api'

interface Props {
  finding: Finding
  onNavigate: (line: number, column: number) => void
}

const severityStyle: Record<string, string> = {
  Error:   'bg-red-100 text-red-700 border-red-200',
  Warning: 'bg-amber-100 text-amber-700 border-amber-200',
  Info:    'bg-blue-100 text-blue-700 border-blue-200',
}

const severityDot: Record<string, string> = {
  Error:   'bg-red-500',
  Warning: 'bg-amber-400',
  Info:    'bg-blue-400',
}

export default function FindingItem({ finding, onNavigate }: Props) {
  const [expanded, setExpanded] = useState(false)
  const hasFix = !!finding.llmSuggestedFix

  return (
    <div className={`rounded-lg border p-3 text-sm ${severityStyle[finding.severity] ?? 'bg-gray-50 border-gray-200'}`}>
      <div className="flex items-start gap-2">
        <span className={`mt-1 flex-shrink-0 h-2 w-2 rounded-full ${severityDot[finding.severity] ?? 'bg-gray-400'}`} />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-mono text-xs font-semibold">{finding.ruleId}</span>
            <button
              onClick={() => onNavigate(finding.line, finding.column)}
              className="text-xs underline opacity-70 hover:opacity-100"
            >
              line {finding.line}
            </button>
            {hasFix && (
              <button
                onClick={() => setExpanded(e => !e)}
                className="ml-auto text-xs font-medium opacity-70 hover:opacity-100"
              >
                {expanded ? 'Hide fix ▲' : 'Show fix ▼'}
              </button>
            )}
          </div>
          <p className="mt-1 font-medium">{finding.title}</p>
          <p className="mt-0.5 opacity-80">{finding.message}</p>
          {finding.llmExplanation && (
            <p className="mt-1 text-xs opacity-70 italic">{finding.llmExplanation}</p>
          )}
          {expanded && finding.llmSuggestedFix && (
            <pre className="mt-2 p-2 bg-white/60 rounded text-xs font-mono whitespace-pre-wrap overflow-x-auto border border-current/20">
              {finding.llmSuggestedFix}
            </pre>
          )}
        </div>
      </div>
    </div>
  )
}
