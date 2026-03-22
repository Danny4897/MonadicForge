using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.SharedKernel.Context;

/// <summary>
/// Immutable per-request context. Flows through every operation — validation,
/// DB, and LLM calls. Replaces AgentContext (not yet in MonadicSharp base).
/// </summary>
public sealed record LeafContext(
    string TenantId,
    string UserId,
    PlanTier Plan,
    CancellationToken CancellationToken = default);
