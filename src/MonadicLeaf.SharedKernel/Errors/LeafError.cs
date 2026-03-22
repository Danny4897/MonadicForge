using MonadicSharp;

namespace MonadicLeaf.SharedKernel.Errors;

/// <summary>
/// Domain error factory for MonadicLeaf.
/// Error.Create(message, code) — note: message is the first parameter in MonadicSharp.
/// </summary>
public static class LeafError
{
    public static Error TenantNotFound(string tenantId) =>
        Error.Create($"Tenant '{tenantId}' not found", "LEAF_TENANT_NOT_FOUND", ErrorType.NotFound);

    public static Error PlanLimitExceeded(string feature, int limit) =>
        Error.Create(
            $"Plan limit reached for '{feature}'. Limit: {limit}",
            "LEAF_PLAN_LIMIT_EXCEEDED",
            ErrorType.Forbidden);

    public static Error UnauthorizedFeature(string feature) =>
        Error.Create(
            $"Feature '{feature}' is not available on your current plan",
            "LEAF_UNAUTHORIZED_FEATURE",
            ErrorType.Forbidden);

    public static Error CodeTooLarge(int maxLength) =>
        Error.Create(
            $"Code exceeds maximum length of {maxLength} characters",
            "LEAF_CODE_TOO_LARGE",
            ErrorType.Validation);

    public static Error AnalysisFailed(string reason) =>
        Error.Create(reason, "LEAF_ANALYSIS_FAILED");

    public static Error InvalidStripeSignature() =>
        Error.Create("Invalid Stripe webhook signature", "LEAF_INVALID_STRIPE_SIGNATURE", ErrorType.Forbidden);
}
