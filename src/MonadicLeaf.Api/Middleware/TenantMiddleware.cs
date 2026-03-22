using System.Security.Claims;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.Api.Middleware;

/// <summary>
/// Extracts tenantId from JWT claims, loads the Tenant from DB,
/// and injects a LeafContext into HttpContext.Items for downstream middleware and controllers.
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext, ITenantsService tenantsService)
    {
        // Unauthenticated requests pass through — auth enforced at controller level
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await _next(httpContext);
            return;
        }

        var tenantId = httpContext.User.FindFirstValue("tenantId");
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var planClaim = httpContext.User.FindFirstValue("plan");

        if (tenantId is null || userId is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                code = "LEAF_AUTH_MISSING",
                message = "JWT must contain 'tenantId' and 'sub' claims"
            });
            return;
        }

        var plan = Enum.TryParse<PlanTier>(planClaim, out var p) ? p : PlanTier.Free;
        var context = new LeafContext(tenantId, userId, plan, httpContext.RequestAborted);

        var tenantResult = await tenantsService.GetByIdAsync(tenantId, context);
        if (tenantResult.IsFailure)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(tenantResult.Error);
            return;
        }

        httpContext.Items["LeafContext"] = context;
        httpContext.Items["Tenant"] = tenantResult.Value;

        await _next(httpContext);
    }
}
