import { useRef, useState, useCallback } from 'react'
import Editor, { type OnMount } from '@monaco-editor/react'
import type { editor } from 'monaco-editor'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import ResultsPanel from '../components/ResultsPanel'
import UpgradeModal from '../components/UpgradeModal'
import type { AnalyzeResult, ApiError } from '../types/api'

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

  const handleMount: OnMount = useCallback((editor) => {
    editorRef.current = editor
    editor.addCommand(
      // Ctrl+Enter = KeyMod.CtrlCmd | KeyCode.Enter = 2048 | 3 = 2051
      2051,
      () => runAnalysis(),
    )
    editor.focus()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const runAnalysis = useCallback(async () => {
    const code = editorRef.current?.getValue() ?? ''
    if (!code.trim()) return

    setLoading(true)
    setError(null)
    try {
      const res = await api.analyze.run(code, 'Playground.cs')
      setResult(res)
      refreshUsage(res.findings.length) // approximate; backend is source of truth
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.code === 'LEAF_PLAN_LIMIT_EXCEEDED') {
        setShowUpgrade(true)
      } else {
        setError(apiErr.message ?? 'Analysis failed')
      }
    } finally {
      setLoading(false)
    }
  }, [refreshUsage])

  const navigateToLine = useCallback((line: number, column: number) => {
    const editor = editorRef.current
    if (!editor) return
    editor.revealLineInCenter(line)
    editor.setPosition({ lineNumber: line, column })
    editor.focus()
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
            className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg bg-leaf-500 hover:bg-leaf-600 disabled:opacity-50 text-white font-semibold transition-colors"
          >
            {loading ? (
              <>
                <span className="h-3 w-3 rounded-full border-2 border-white/40 border-t-white animate-spin" />
                Analyzing…
              </>
            ) : (
              <>▶ Analyze <kbd className="opacity-60 text-[10px]">Ctrl+↵</kbd></>
            )}
          </button>
        </div>

        {error && (
          <div className="flex-shrink-0 px-4 py-2 bg-red-50 border-b border-red-100 text-xs text-red-600">
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

      {showUpgrade && <UpgradeModal onClose={() => setShowUpgrade(false)} />}
    </div>
  )
}
