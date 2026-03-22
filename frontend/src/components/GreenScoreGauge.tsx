interface Props {
  score: number // 0–100
}

// CSS variable values from tailwind.config leaf tokens
const SCORE_COLORS = {
  high:   '#2D7D46', // leaf-500
  medium: '#f59e0b', // amber-400
  low:    '#ef4444', // red-500
} as const

const TRACK_COLOR = '#e5e7eb' // gray-200
const LABEL_COLOR = '#6b7280' // gray-500

function scoreColor(score: number): string {
  if (score >= 80) return SCORE_COLORS.high
  if (score >= 60) return SCORE_COLORS.medium
  return SCORE_COLORS.low
}

export default function GreenScoreGauge({ score }: Props) {
  const r = 40
  const circ = 2 * Math.PI * r
  const arc = circ * 0.75
  const offset = arc - (arc * score) / 100
  const color = scoreColor(score)

  return (
    <div className="flex flex-col items-center gap-1" role="img" aria-label={`Green Score: ${score} out of 100`}>
      <svg width="120" height="100" viewBox="0 0 120 100">
        {/* Background track */}
        <circle
          cx="60" cy="65" r={r}
          fill="none" stroke={TRACK_COLOR} strokeWidth="10"
          strokeDasharray={`${arc} ${circ - arc}`}
          strokeDashoffset={0}
          strokeLinecap="round"
          transform="rotate(135 60 65)"
        />
        {/* Score arc */}
        <circle
          cx="60" cy="65" r={r}
          fill="none" stroke={color} strokeWidth="10"
          strokeDasharray={`${arc} ${circ - arc}`}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform="rotate(135 60 65)"
          style={{ transition: 'stroke-dashoffset 0.5s ease' }}
        />
        <text x="60" y="68" textAnchor="middle" fontSize="22" fontWeight="700" fill={color}>
          {score}
        </text>
        <text x="60" y="84" textAnchor="middle" fontSize="10" fill={LABEL_COLOR}>
          Green Score
        </text>
      </svg>
    </div>
  )
}
