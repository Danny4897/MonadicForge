export interface AuthResponse {
  token: string
  email: string
  tenantId: string
  plan: string
  analysesUsedThisMonth: number
  analysesPerMonth: number
}

export interface MeResponse {
  userId: string
  tenantId: string
  plan: string
  analysesUsedThisMonth: number
  analysesPerMonth: number
}

export type Severity = 'Error' | 'Warning' | 'Info'

export interface Finding {
  id: string
  ruleId: string
  title: string
  severity: Severity
  line: number
  column: number
  message: string
  llmExplanation?: string
  llmSuggestedFix?: string
}

export interface AnalyzeResult {
  recordId: string
  greenScore: number
  findings: Finding[]
  fileName?: string
}

export interface AnalysisRecord {
  recordId: string
  fileName?: string
  greenScore: number
  findingCount: number
  createdAt: string
}

export interface ApiError {
  code: string
  message: string
}
