interface Props {
  score: number // 0–100
}

function scoreColor(score: number) {
  if (score >= 80) return '#2D7D46'
  if (score >= 60) return '#f59e0b'
  return '#ef4444'
}

export default function GreenScoreGauge({ score }: Props) {
  const r = 40
  const circ = 2 * Math.PI * r
  const arc = circ * 0.75           // 270° arc
  const offset = arc - (arc * score) / 100
  const color = scoreColor(score)

  return (
    <div className="flex flex-col items-center gap-1">
      <svg width="120" height="100" viewBox="0 0 120 100">
        {/* Background track */}
        <circle
          cx="60" cy="65" r={r}
          fill="none" stroke="#e5e7eb" strokeWidth="10"
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
        <text x="60" y="84" textAnchor="middle" fontSize="10" fill="#6b7280">
          Green Score
        </text>
      </svg>
    </div>
  )
}
