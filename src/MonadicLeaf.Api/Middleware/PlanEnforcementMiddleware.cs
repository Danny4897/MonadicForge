using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Errors;
using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.Api.Middleware;

/// <summary>
/// Gates feature endpoints by plan tier.
/// Runs after TenantMiddleware — requires LeafContext in HttpContext.Items.
/// Returns 403 with a LeafError body when the plan doesn't include the feature.
/// </summary>
public sealed class PlanEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    // Map path prefix → feature gate. Checked with StartsWith for flexibility.
    private static readonly (string Prefix, Func<PlanConfig, bool> IsEnabled)[] FeatureGates =
    [
        ("/api/generate", cfg => cfg.GenerateEnabled),
        ("/api/review",   cfg => cfg.ReviewEnabled),
    ];

    public PlanEnforcementMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var context = httpContext.Items["LeafContext"] as LeafContext;
        if (context is null)
        {
            await _next(httpContext);
            return;
        }

        var path = httpContext.Request.Path.Value ?? "";
        var config = PlanLimits.Plans[context.Plan];

        foreach (var (prefix, isEnabled) in FeatureGates)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !isEnabled(config))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(
                    LeafError.UnauthorizedFeature(prefix.TrimStart('/')));
                return;
            }
        }

        await _next(httpContext);
    }
}
