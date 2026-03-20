# MonadicForge

[![Build](https://github.com/Danny4897/MonadicForge/actions/workflows/build.yml/badge.svg)](https://github.com/Danny4897/MonadicForge/actions)
[![NuGet](https://img.shields.io/nuget/v/MonadicForge.svg)](https://www.nuget.org/packages/MonadicForge)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **The structural guarantee that AI-generated C# code doesn't break in production.**

MonadicForge is a CLI tool that statically analyzes and auto-migrates C# code to ensure it follows [MonadicSharp](https://github.com/Danny4897/MonadicSharp) green-code principles: Railway-Oriented Programming, safe error handling, and zero wasted compute.

## Quick Start

```bash
dotnet tool install -g MonadicForge

dotnet forge analyze --path ./src
dotnet forge migrate --path ./src
dotnet forge report  --path ./src --output report.html
```

## Commands

| Command | Description |
|---------|-------------|
| `forge analyze <path>` | Static analysis — finds green-code violations |
| `forge migrate <path>` | Auto-rewrites code to MonadicSharp patterns |
| `forge report  <path>` | Generates HTML report with Green Score gauge |

All commands accept `--path` (default: current directory).

## The 10 Rules

| RuleId | Severity | Description |
|--------|----------|-------------|
| **GC001** | 🔴 Error   | `try/catch` inside `Bind` breaks the railway — use `Try.ExecuteAsync` |
| **GC002** | 🔴 Error   | `Map` on fallible operations — use `Bind + Try.Execute` |
| **GC003** | 🟡 Warning | Expensive I/O before cheap validation — reorder the chain |
| **GC004** | 🟡 Warning | `WithRetry` without `useJitter: true` — thundering herd risk |
| **GC005** | 🟡 Warning | Validation inside retry scope — terminal errors waste retry cycles |
| **GC006** | 🟡 Warning | LLM calls without `CachingAgentWrapper` — repeated tokens burned |
| **GC007** | 🔴 Error   | LLM output used without `ValidatedResult` at the boundary |
| **GC008** | 🟡 Warning | `AgentCapability.All` granted — use minimum required capabilities |
| **GC009** | 🟡 Warning | External agents without `CircuitBreaker` |
| **GC010** | 🔵 Info    | `Sequence()` on large collections — use `Partition()` for batch processing |

## How It Works — Before / After

### GC001: try/catch inside Bind

```csharp
// BEFORE — breaks the railway, exception swallowed silently
result.Bind(order =>
{
    try { return Result<Order>.Success(await Save(order)); }
    catch (Exception ex) { return Result<Order>.Failure(Error.FromException(ex)); }
});

// AFTER — clean railway, error is a first-class value
result.BindAsync(order => Try.ExecuteAsync(() => Save(order)));
```

### GC004: WithRetry without jitter

```csharp
// BEFORE — all retries fire simultaneously, thundering herd
pipeline.WithRetry(maxAttempts: 3);

// AFTER — staggered retries
pipeline.WithRetry(maxAttempts: 3, useJitter: true);
```

### GC007: LLM output without validation

```csharp
// BEFORE — raw AI text used directly
var content = await _chat.CompleteAsync(prompt);
var order = JsonSerializer.Deserialize<Order>(content);

// AFTER — validated at the boundary
var result = await Try.ExecuteAsync(() => _chat.CompleteAsync(prompt));
return result
    .Ensure(r => !string.IsNullOrWhiteSpace(r.Content), "Empty LLM output")
    .Bind(r => Try.Execute(() => JsonSerializer.Deserialize<Order>(r.Content)!));
```

## Green Score

Every run produces a **Green Score** (0–100):

| Penalty | Points |
|---------|--------|
| Error   | -10    |
| Warning | -5     |
| Info    | -1     |

A score of 90+ means your codebase is production-safe by MonadicSharp standards.

## Auto-Migration Rules

Run `forge migrate` to automatically apply:

| Rule | Transformation |
|------|---------------|
| **M001** | `try/catch → Result` pattern → `Try.ExecuteAsync` |
| **M002** | `.Map(fallible)` → `.Bind(x => Try.Execute(fallible))` |
| **M003** | `.WithRetry(...)` → adds `useJitter: true` |
| **M004** | `return null` in nullable methods → `Option<T>.None` |

All migrations are **idempotent** — applying twice produces the same result.

## Why MonadicSharp

MonadicForge is built on top of [MonadicSharp](https://github.com/Danny4897/MonadicSharp) — a production-grade functional library for .NET featuring:

- `Result<T>` — Railway-Oriented Programming with typed errors
- `Option<T>` — Null-safe optionals
- `Try.ExecuteAsync` — Exception-free async I/O
- `AgentContext` — Capability-based security for AI agents
- `CachingAgentWrapper` — Transparent LLM call caching

```bash
dotnet add package MonadicSharp
```

## Architecture

```
MonadicForge/
├── src/
│   ├── MonadicForge.Cli/          # System.CommandLine + Spectre.Console
│   ├── MonadicForge.Analyzer/     # 10 Roslyn SyntaxWalker rules
│   ├── MonadicForge.Migrator/     # 4 Roslyn SyntaxRewriter migrations
│   └── MonadicForge.Report/       # HTML report + Green Score
└── tests/
    ├── MonadicForge.Analyzer.Tests/
    └── MonadicForge.Migrator.Tests/
```

## License

MIT © [Danny4897](https://github.com/Danny4897)
