import type { AnalyzeResult, Severity } from '../types/api'
import GreenScoreGauge from './GreenScoreGauge'
import FindingItem from './FindingItem'

interface Props {
  result: AnalyzeResult | null
  loading: boolean
  onNavigate: (line: number, column: number) => void
}

const SEVERITIES: Severity[] = ['Error', 'Warning', 'Info']

export default function ResultsPanel({ result, loading, onNavigate }: Props) {
  if (loading) {
    return (
      <div
        className="flex flex-col items-center justify-center h-full gap-3 text-gray-400"
        role="status"
        aria-busy="true"
        aria-label="Analyzing code"
      >
        <div className="h-8 w-8 rounded-full border-4 border-leaf-200 border-t-leaf-500 animate-spin" />
        <span className="text-sm">Analyzing…</span>
      </div>
    )
  }

  if (!result) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-2 text-gray-400 px-6 text-center">
        <span className="text-4xl" aria-hidden="true">🌿</span>
        <p className="text-sm">
          Paste your C# code and press{' '}
          <kbd className="px-1 py-0.5 bg-gray-100 rounded text-xs font-mono">Ctrl+Enter</kbd>{' '}
          to analyze.
        </p>
      </div>
    )
  }

  const grouped = SEVERITIES.reduce<Record<Severity, typeof result.findings>>((acc, sev) => {
    acc[sev] = result.findings.filter(f => f.severity === sev)
    return acc
  }, { Error: [], Warning: [], Info: [] })

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Score header */}
      <div className="flex-shrink-0 flex items-center gap-4 p-4 border-b border-gray-100">
        <GreenScoreGauge score={result.greenScore} />
        <div className="flex flex-col text-sm text-gray-600">
          <span className="font-semibold text-gray-800">{result.findings.length} findings</span>
          {result.fileName && (
            <span className="text-xs text-gray-400 font-mono">{result.fileName}</span>
          )}
          <div className="mt-1 flex gap-2 text-xs">
            {grouped.Error.length > 0 && (
              <span className="text-red-600">{grouped.Error.length} errors</span>
            )}
            {grouped.Warning.length > 0 && (
              <span className="text-amber-600">{grouped.Warning.length} warnings</span>
            )}
            {grouped.Info.length > 0 && (
              <span className="text-blue-600">{grouped.Info.length} info</span>
            )}
          </div>
        </div>
      </div>

      {/* Findings list */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {result.findings.length === 0 && (
          <div className="text-center text-sm text-gray-400 py-8">
            <span className="text-2xl block mb-2" aria-hidden="true">✅</span>
            No findings — your code looks green!
          </div>
        )}
        {SEVERITIES.map(sev =>
          grouped[sev].length > 0 ? (
            <div key={sev}>
              <h3 className="text-xs font-semibold uppercase tracking-wide text-gray-400 mb-2">
                {sev}s
              </h3>
              <div className="space-y-2">
                {grouped[sev].map(f => (
                  <FindingItem key={f.id} finding={f} onNavigate={onNavigate} />
                ))}
              </div>
            </div>
          ) : null
        )}
      </div>
    </div>
  )
}
