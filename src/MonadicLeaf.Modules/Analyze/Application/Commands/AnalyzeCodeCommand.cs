using System.Text;
using System.Text.Json.Serialization;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Modules.Analyze.Application.Llm;
using MonadicLeaf.Modules.Analyze.Contracts;
using MonadicLeaf.Modules.Analyze.Domain.Entities;
using MonadicLeaf.Modules.Analyze.Domain.ValueObjects;
using MonadicLeaf.Modules.Analyze.Infrastructure.Persistence;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Errors;
using MonadicLeaf.SharedKernel.Plan;
using MonadicLeaf.SharedKernel.Retry;
using MonadicLeaf.SharedKernel.Validation;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Analyze.Application.Commands;

public sealed class AnalyzeCodeCommand
{
    private readonly AnalysisEngine _engine;
    private readonly AnthropicClient _llm;
    private readonly AnalysisRepository _repo;
    private readonly ITenantsService _tenants;

    public AnalyzeCodeCommand(
        AnalysisEngine engine,
        AnthropicClient llm,
        AnalysisRepository repo,
        ITenantsService tenants)
    {
        _engine = engine;
        _llm = llm;
        _repo = repo;
        _tenants = tenants;
    }

    /// <summary>
    /// Green-code pipeline — cheapest operations first:
    /// validate → plan check (DB) → Roslyn (CPU) → LLM (expensive) → persist → increment usage.
    /// </summary>
    public Task<Result<AnalyzeResult>> ExecuteAsync(AnalyzeRequest request, LeafContext context) =>
        // 1. Cheapest: validate inputs — no I/O
        ValidateRequest(request, context)

        // 2. Plan limit check — DB read
        .BindAsync(r => CheckPlanLimit(r, context))

        // 3. Roslyn analysis — CPU only, no LLM
        // AsTask() converts Result<T> → Task<Result<T>> so Bind can chain it
        .Bind(r => RunAnalysis(r).AsTask())

        // 4. Score calculation + LLM enrichment (combined to avoid anonymous tuple inference)
        .BindAsync(findings => ScoreAndEnrich(findings, request, context))

        // 5. Persist result — Try.ExecuteAsync inside repo, never try/catch here
        .BindAsync(result => SaveResult(result, request, context))

        // 6. Increment usage counter after successful persist
        .BindAsync(result => IncrementAndReturn(result, context));

    // ─── Pipeline steps ────────────────────────────────────────────────────────

    private static Result<AnalyzeRequest> ValidateRequest(AnalyzeRequest request, LeafContext context)
    {
        var config = PlanLimits.Plans[context.Plan];
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<AnalyzeRequest>.Failure(
                Error.Validation("Code cannot be empty", "code"));
        if (request.Code.Length > config.MaxCodeLength)
            return Result<AnalyzeRequest>.Failure(
                LeafError.CodeTooLarge(config.MaxCodeLength));
        return Result<AnalyzeRequest>.Success(request);
    }

    private async Task<Result<AnalyzeRequest>> CheckPlanLimit(AnalyzeRequest request, LeafContext context)
    {
        var tenantResult = await _tenants.GetByIdAsync(context.TenantId, context);
        if (tenantResult.IsFailure)
            return Result<AnalyzeRequest>.Failure(tenantResult.Error);

        return tenantResult.Value.HasExceededMonthlyLimit()
            ? Result<AnalyzeRequest>.Failure(
                LeafError.PlanLimitExceeded("analyses",
                    PlanLimits.Plans[context.Plan].AnalysesPerMonth))
            : Result<AnalyzeRequest>.Success(request);
    }

    private Result<IReadOnlyList<EnrichedFinding>> RunAnalysis(AnalyzeRequest request) =>
        _engine
            .AnalyzeSource(request.Code, request.FileName ?? "<inline>")
            .Map(findings => (IReadOnlyList<EnrichedFinding>)
                findings.Select(EnrichedFinding.FromRoslyn).ToList());

    private async Task<Result<AnalyzeResult>> ScoreAndEnrich(
        IReadOnlyList<EnrichedFinding> findings,
        AnalyzeRequest request,
        LeafContext context)
    {
        var score = CalculateGreenScore(findings);

        // Skip enrichment if no findings or LLM not configured — degrade gracefully
        if (findings.Count == 0 || !_llm.IsConfigured)
            return Result<AnalyzeResult>.Success(
                new AnalyzeResult(Guid.Empty, score, findings, request.FileName));

        var prompt = BuildEnrichmentPrompt(findings);

        // Green-code: WithRetry outside validation scope, useJitter: true
        var llmResult = await RetryHelper.WithRetry(
            () => _llm.CompleteAsync(SystemPrompt, prompt, context.CancellationToken),
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            useJitter: true);

        // LLM failure → graceful degrade to raw findings (no error propagation)
        if (llmResult.IsFailure)
            return Result<AnalyzeResult>.Success(
                new AnalyzeResult(Guid.Empty, score, findings, request.FileName));

        // Green-code: ValidatedResult at LLM output boundary
        var enrichment = await llmResult
            .ParseAs<EnrichmentRaw>()
            .Validate(r => r.Enrichments is not null, "Enrichments array required")
            .AsResultAsync();

        var enrichedFindings = enrichment.IsSuccess
            ? MergeEnrichments(findings, enrichment.Value.Enrichments!)
            : findings; // LLM output invalid → use raw findings

        return Result<AnalyzeResult>.Success(
            new AnalyzeResult(Guid.Empty, score, enrichedFindings, request.FileName));
    }

    private Task<Result<AnalyzeResult>> SaveResult(
        AnalyzeResult result, AnalyzeRequest request, LeafContext context) =>
        _repo.SaveAsync(
            AnalysisRecord.Create(
                context.TenantId, context.UserId,
                request.FileName, result.GreenScore, result.Findings),
            context)
        .Map(record => result with { RecordId = record.Id });

    private async Task<Result<AnalyzeResult>> IncrementAndReturn(
        AnalyzeResult result, LeafContext context)
    {
        var inc = await _tenants.IncrementUsageAsync(context.TenantId, context);
        return inc.IsFailure
            ? Result<AnalyzeResult>.Failure(inc.Error)
            : Result<AnalyzeResult>.Success(result);
    }

    // ─── Pure helpers ──────────────────────────────────────────────────────────

    private static int CalculateGreenScore(IReadOnlyList<EnrichedFinding> findings)
    {
        // From CLAUDE.md: Error (Critical)=-15, Warning=-7, Info=-3, minimum=0
        var penalty = findings.Sum(f => f.Severity switch
        {
            FindingSeverity.Error   => 15,
            FindingSeverity.Warning => 7,
            FindingSeverity.Info    => 3,
            _                       => 0
        });
        return Math.Max(0, 100 - penalty);
    }

    private static string BuildEnrichmentPrompt(IReadOnlyList<EnrichedFinding> findings)
    {
        var sb = new StringBuilder("Findings to enrich:\n");
        foreach (var f in findings)
            sb.AppendLine($"- ruleId: {f.RuleId}, severity: {f.Severity}, description: {f.Description}");
        return sb.ToString();
    }

    private static IReadOnlyList<EnrichedFinding> MergeEnrichments(
        IReadOnlyList<EnrichedFinding> findings, List<EnrichmentItem> enrichments)
    {
        var map = enrichments.ToDictionary(e => e.RuleId, StringComparer.OrdinalIgnoreCase);
        return findings
            .Select(f => map.TryGetValue(f.RuleId, out var e)
                ? f.WithLlmEnrichment(e.Explanation, e.SuggestedFix)
                : f)
            .ToList();
    }

    // ─── LLM system prompt ────────────────────────────────────────────────────

    private const string SystemPrompt = """
        You are a MonadicSharp green-code auditor for C#.

        You receive a list of Roslyn-detected violations. Your job is to enrich each
        violation with a clear, developer-friendly explanation and a concrete suggested fix.

        Return ONLY a valid JSON object — no preamble, no markdown fences:

        {
          "enrichments": [
            {
              "ruleId": "<string, e.g. GC001>",
              "explanation": "<string, max 120 chars, explain WHY this wastes compute>",
              "suggestedFix": "<string, unified diff format, show exact before/after>"
            }
          ]
        }

        Green-code rules reference:
        GC001 Cheapest Bind first — validation before I/O, I/O before LLM
        GC002 Map only for infallible transforms — if throws, use Bind + Try.ExecuteAsync
        GC003 WithRetry outside validation scope — never retry validation; always useJitter:true
        GC004 CachingAgentWrapper on every repeated LLM agent
        GC005 ValidatedResult<T> at every LLM output boundary
        GC006 Try.ExecuteAsync at I/O — no try/catch inside Bind lambdas
        GC007 Minimum AgentCapability — never AgentCapability.All in production
        GC008 CircuitBreaker on every external service agent
        GC009 Partition over Sequence for batch operations
        GC010 Token budget check before every LLM call

        Be precise. Explain the compute cost, not just the pattern name.
        """;

    // ─── LLM response DTOs ────────────────────────────────────────────────────

    private sealed class EnrichmentRaw
    {
        [JsonPropertyName("enrichments")]
        public List<EnrichmentItem>? Enrichments { get; set; }
    }

    private sealed class EnrichmentItem
    {
        [JsonPropertyName("ruleId")]
        public string RuleId { get; set; } = default!;

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("suggestedFix")]
        public string? SuggestedFix { get; set; }
    }
}
