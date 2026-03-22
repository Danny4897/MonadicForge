import { useRef, useState, useCallback, useEffect } from 'react'
import Editor, { type OnMount } from '@monaco-editor/react'
import type { editor } from 'monaco-editor'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import ResultsPanel from '../components/ResultsPanel'
import UpgradeModal from '../components/UpgradeModal'
import type { AnalyzeResult } from '../types/api'

function extractMessage(err: unknown, fallback: string): string {
  if (err && typeof err === 'object' && 'code' in err && err.code === 'LEAF_PLAN_LIMIT_EXCEEDED') {
    return ''
  }
  if (err && typeof err === 'object' && 'message' in err && typeof err.message === 'string') {
    return err.message
  }
  return fallback
}

function isLimitError(err: unknown): boolean {
  return !!(err && typeof err === 'object' && 'code' in err && err.code === 'LEAF_PLAN_LIMIT_EXCEEDED')
}

const PLACEHOLDER = `// Paste your C# code here and press Ctrl+Enter to analyze
using System;

public class Example
{
    public string GetName(object input)
    {
        try
        {
            return ((MyClass)input).Name;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}
`

export default function Playground() {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const [result, setResult] = useState<AnalyzeResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showUpgrade, setShowUpgrade] = useState(false)
  const { token, refreshUsage } = useAuth()

  const handleCloseUpgrade = useCallback(() => setShowUpgrade(false), [])

  const runAnalysis = useCallback(async () => {
    const code = editorRef.current?.getValue() ?? ''
    if (!code.trim()) return

    setLoading(true)
    setError(null)
    try {
      const res = await api.analyze.run(code, 'Playground.cs')
      setResult(res)
      refreshUsage(res.findings.length)
    } catch (err) {
      if (isLimitError(err)) {
        setShowUpgrade(true)
      } else {
        setError(extractMessage(err, 'Analysis failed. Please try again.'))
      }
    } finally {
      setLoading(false)
    }
  }, [refreshUsage])

  const handleMount: OnMount = useCallback((ed) => {
    editorRef.current = ed
    ed.focus()
  }, [])

  // Wire Ctrl+Enter after mount so runAnalysis is never stale
  useEffect(() => {
    const editor = editorRef.current
    if (!editor) return
    // Ctrl+Enter = KeyMod.CtrlCmd (2048) | KeyCode.Enter (3) = 2051
    editor.addCommand(2051, () => runAnalysis())

  }, [runAnalysis])

  const navigateToLine = useCallback((line: number, column: number) => {
    const ed = editorRef.current
    if (!ed) return
    ed.revealLineInCenter(line)
    ed.setPosition({ lineNumber: line, column })
    ed.focus()
  }, [])

  return (
    <div className="flex flex-1 overflow-hidden">
      {/* ── Left: Monaco Editor ── */}
      <div className="flex flex-col flex-1 min-w-0 border-r border-gray-100">
        {/* Toolbar */}
        <div className="flex-shrink-0 flex items-center justify-between px-4 py-2 bg-gray-50 border-b border-gray-100">
          <span className="text-xs text-gray-400 font-mono">Playground.cs</span>
          <button
            onClick={runAnalysis}
            disabled={loading}
            aria-disabled={loading}
            className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 disabled:opacity-50 text-white font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-leaf-500"
          >
            {loading ? (
              <>
                <span className="h-3 w-3 rounded-full border-2 border-white/30 border-t-white animate-spin" aria-hidden="true" />
                Analyzing…
              </>
            ) : (
              <>▶ Analyze <kbd className="opacity-60 text-[10px]">Ctrl+↵</kbd></>
            )}
          </button>
        </div>

        {error && (
          <div className="flex-shrink-0 px-4 py-2 bg-red-50 border-b border-red-100 text-xs text-red-600" role="alert">
            {error}
          </div>
        )}

        {!token && (
          <div className="flex-shrink-0 px-4 py-2 bg-amber-50 border-b border-amber-100 text-xs text-amber-700">
            Sign in to save your analyses and get AI explanations.
          </div>
        )}

        <div className="flex-1 overflow-hidden">
          <Editor
            defaultValue={PLACEHOLDER}
            defaultLanguage="csharp"
            theme="vs-light"
            onMount={handleMount}
            options={{
              fontSize: 14,
              fontFamily: "'Cascadia Code', 'Fira Code', monospace",
              minimap: { enabled: false },
              scrollBeyondLastLine: false,
              padding: { top: 16, bottom: 16 },
              wordWrap: 'on',
              renderLineHighlight: 'gutter',
            }}
          />
        </div>
      </div>

      {/* ── Right: Results ── */}
      <div className="w-96 flex-shrink-0 flex flex-col overflow-hidden bg-white">
        <ResultsPanel result={result} loading={loading} onNavigate={navigateToLine} />
      </div>

      {showUpgrade && <UpgradeModal onClose={handleCloseUpgrade} />}
    </div>
  )
}
