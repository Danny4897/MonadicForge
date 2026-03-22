using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonadicLeaf.Api.Extensions;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.SharedKernel.Context;

namespace MonadicLeaf.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantsService _tenantsService;

    public TenantsController(ITenantsService tenantsService) =>
        _tenantsService = tenantsService;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var context = HttpContext.Items["LeafContext"] as LeafContext;
        if (context is null) return Unauthorized();

        var result = await _tenantsService.GetByIdAsync(context.TenantId, context);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        var context = HttpContext.Items["LeafContext"] as LeafContext;
        if (context is null) return Unauthorized();

        var result = await _tenantsService.CreateAsync(request.Name, context);
        return result.Match<IActionResult>(
            onSuccess: tenant => CreatedAtAction(nameof(GetMe), tenant),
            onFailure: _ => result.ToActionResult());
    }
}

public sealed record CreateTenantRequest(string Name);
