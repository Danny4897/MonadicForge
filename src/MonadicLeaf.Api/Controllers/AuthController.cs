using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonadicLeaf.Api.Extensions;
using MonadicLeaf.Modules.Auth.Contracts;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ITenantsService _tenants;

    public AuthController(IAuthService auth, ITenantsService tenants)
    {
        _auth = auth;
        _tenants = tenants;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        var result = await _auth.RegisterAsync(request.Email, request.Password);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        var result = await _auth.LoginAsync(request.Email, request.Password);
        return result.ToActionResult();
    }

    /// <summary>Returns current user info including usage stats.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var context = HttpContext.Items["LeafContext"] as LeafContext;
        if (context is null) return Unauthorized();

        var tenantResult = await _tenants.GetByIdAsync(context.TenantId, context);
        if (tenantResult.IsFailure) return tenantResult.ToActionResult();

        var tenant = tenantResult.Value;
        var config = PlanLimits.Plans[tenant.Plan];

        return Ok(new MeResponse(
            context.UserId,
            context.TenantId,
            tenant.Plan.ToString(),
            tenant.AnalysesUsedThisMonth,
            config.AnalysesPerMonth == int.MaxValue ? -1 : config.AnalysesPerMonth));
    }
}

public sealed record AuthRequest(string Email, string Password);

public sealed record MeResponse(
    string UserId,
    string TenantId,
    string Plan,
    int AnalysesUsedThisMonth,
    int AnalysesPerMonth);
