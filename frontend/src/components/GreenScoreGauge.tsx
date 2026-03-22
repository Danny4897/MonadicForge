interface Props {
  score: number // 0–100
  size?: 'sm' | 'lg'
}

const SCORE_COLORS = {
  high:   '#2D7D46', // leaf-500
  medium: '#f59e0b', // amber-400
  low:    '#ef4444', // red-500
} as const

const TRACK_COLOR = '#e5e7eb'
const LABEL_COLOR = '#9ca3af'

function scoreColor(score: number): string {
  if (score >= 80) return SCORE_COLORS.high
  if (score >= 60) return SCORE_COLORS.medium
  return SCORE_COLORS.low
}

function scoreLabel(score: number): string {
  if (score >= 90) return 'Excellent'
  if (score >= 80) return 'Good'
  if (score >= 60) return 'Fair'
  if (score >= 40) return 'Poor'
  return 'Critical'
}

export default function GreenScoreGauge({ score, size = 'sm' }: Props) {
  const isLarge = size === 'lg'
  const r = isLarge ? 52 : 38
  const sw = isLarge ? 10 : 8
  const w = isLarge ? 150 : 110
  const h = isLarge ? 125 : 92
  const cx = w / 2
  const cy = isLarge ? 85 : 62

  const circ = 2 * Math.PI * r
  const arc = circ * 0.75
  const offset = arc - (arc * score) / 100
  const color = scoreColor(score)

  return (
    <div className="flex flex-col items-center" role="img" aria-label={`Green Score: ${score} out of 100`}>
      <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`}>
        {/* Shadow filter */}
        <defs>
          <filter id="glow">
            <feGaussianBlur stdDeviation="2" result="blur" />
            <feComposite in="SourceGraphic" in2="blur" operator="over" />
          </filter>
        </defs>
        {/* Background track */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none" stroke={TRACK_COLOR} strokeWidth={sw}
          strokeDasharray={`${arc} ${circ - arc}`}
          strokeLinecap="round"
          transform={`rotate(135 ${cx} ${cy})`}
        />
        {/* Score arc */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none" stroke={color} strokeWidth={sw}
          strokeDasharray={`${arc} ${circ - arc}`}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform={`rotate(135 ${cx} ${cy})`}
          style={{ transition: 'stroke-dashoffset 0.6s cubic-bezier(0.4,0,0.2,1)' }}
        />
        {/* Score number */}
        <text
          x={cx} y={isLarge ? cy + 2 : cy + 1}
          textAnchor="middle"
          fontSize={isLarge ? 30 : 22}
          fontWeight="700"
          fill={color}
          fontFamily="system-ui, sans-serif"
        >
          {score}
        </text>
        {/* /100 subscript */}
        <text
          x={cx} y={isLarge ? cy + 18 : cy + 14}
          textAnchor="middle"
          fontSize={isLarge ? 11 : 9}
          fill={LABEL_COLOR}
          fontFamily="system-ui, sans-serif"
        >
          /100 · {scoreLabel(score)}
        </text>
      </svg>
    </div>
  )
}
