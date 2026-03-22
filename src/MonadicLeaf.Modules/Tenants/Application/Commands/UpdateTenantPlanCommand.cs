using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Plan;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Tenants.Application.Commands;

public sealed class UpdateTenantPlanCommand
{
    private readonly TenantRepository _repo;

    public UpdateTenantPlanCommand(TenantRepository repo) => _repo = repo;

    public Task<Result<Tenant>> ExecuteAsync(string tenantId, PlanTier newPlan, LeafContext context) =>
        _repo.GetByIdAsync(tenantId, context)
             // Green-code: Map only for infallible transforms — UpdatePlan cannot throw
             .Map(tenant => { tenant.UpdatePlan(newPlan); return tenant; })
             .Bind(tenant => _repo.UpdateAsync(tenant, context));
}
