import type { AnalyzeResult, Severity } from '../types/api'
import GreenScoreGauge from './GreenScoreGauge'
import FindingItem from './FindingItem'

interface Props {
  result: AnalyzeResult | null
  loading: boolean
  onNavigate: (line: number, column: number) => void
}

const SEVERITIES: Severity[] = ['Error', 'Warning', 'Info']

const SEV_PILL: Record<Severity, string> = {
  Error:   'bg-red-100 text-red-700',
  Warning: 'bg-amber-100 text-amber-700',
  Info:    'bg-blue-100 text-blue-700',
}

export default function ResultsPanel({ result, loading, onNavigate }: Props) {
  if (loading) {
    return (
      <div
        className="flex flex-col items-center justify-center h-full gap-4 text-gray-400 bg-gray-50/50"
        role="status"
        aria-busy="true"
        aria-label="Analyzing code"
      >
        <div className="relative">
          <div className="h-12 w-12 rounded-full border-4 border-leaf-100 border-t-leaf-500 animate-spin" />
          <div className="absolute inset-0 flex items-center justify-center text-leaf-500 text-lg">🌿</div>
        </div>
        <div className="text-center">
          <p className="text-sm font-medium text-gray-600">Analyzing…</p>
          <p className="text-xs text-gray-400 mt-0.5">Running Roslyn rules</p>
        </div>
      </div>
    )
  }

  if (!result) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-3 text-gray-400 px-8 text-center bg-gray-50/30">
        <div className="w-16 h-16 rounded-2xl bg-leaf-50 border border-leaf-100 flex items-center justify-center text-3xl">
          🌿
        </div>
        <div>
          <p className="text-sm font-medium text-gray-600">Ready to analyze</p>
          <p className="text-xs text-gray-400 mt-1">
            Paste C# code and press{' '}
            <kbd className="px-1.5 py-0.5 bg-white border border-gray-200 rounded text-xs font-mono shadow-sm">
              Ctrl+Enter
            </kbd>
          </p>
        </div>
      </div>
    )
  }

  const grouped = SEVERITIES.reduce<Record<Severity, typeof result.findings>>((acc, sev) => {
    acc[sev] = result.findings.filter(f => f.severity === sev)
    return acc
  }, { Error: [], Warning: [], Info: [] })

  const scoreGradient = result.greenScore >= 80
    ? 'from-leaf-50 to-white'
    : result.greenScore >= 60
    ? 'from-amber-50 to-white'
    : 'from-red-50 to-white'

  return (
    <div className="flex flex-col h-full overflow-hidden">
      {/* Score hero */}
      <div className={`flex-shrink-0 bg-gradient-to-b ${scoreGradient} px-4 pt-4 pb-3 border-b border-gray-100`}>
        <div className="flex items-center gap-3">
          <GreenScoreGauge score={result.greenScore} />
          <div className="flex flex-col gap-1.5">
            {result.fileName && (
              <span className="text-[10px] text-gray-400 font-mono bg-gray-100 px-1.5 py-0.5 rounded w-fit">
                {result.fileName}
              </span>
            )}
            <div className="flex flex-wrap gap-1">
              {SEVERITIES.map(sev =>
                grouped[sev].length > 0 ? (
                  <span key={sev} className={`text-xs px-2 py-0.5 rounded-full font-medium ${SEV_PILL[sev]}`}>
                    {grouped[sev].length} {sev.toLowerCase()}{grouped[sev].length > 1 ? 's' : ''}
                  </span>
                ) : null
              )}
              {result.findings.length === 0 && (
                <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-leaf-100 text-leaf-700">
                  0 findings
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Findings list */}
      <div className="flex-1 overflow-y-auto p-3 space-y-3">
        {result.findings.length === 0 && (
          <div className="flex flex-col items-center text-center py-10 gap-2">
            <div className="w-12 h-12 rounded-xl bg-leaf-50 border border-leaf-100 flex items-center justify-center text-2xl">
              ✅
            </div>
            <p className="text-sm font-medium text-gray-700">Looks green!</p>
            <p className="text-xs text-gray-400">No MonadicSharp violations found</p>
          </div>
        )}
        {SEVERITIES.map(sev =>
          grouped[sev].length > 0 ? (
            <div key={sev}>
              <h3 className="text-[10px] font-semibold uppercase tracking-widest text-gray-400 mb-1.5 px-1">
                {sev}s
              </h3>
              <div className="space-y-1.5">
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
