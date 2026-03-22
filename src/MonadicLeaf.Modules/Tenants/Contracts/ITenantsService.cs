using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Plan;
using MonadicSharp;

namespace MonadicLeaf.Modules.Tenants.Contracts;

/// <summary>
/// Only public surface of the Tenants module.
/// All other types in Tenants/ are internal to the module.
/// </summary>
public interface ITenantsService
{
    Task<Result<Tenant>> GetByIdAsync(string tenantId, LeafContext context);
    Task<Result<Tenant>> CreateAsync(string name, LeafContext context);
    Task<Result<Tenant>> UpdatePlanAsync(string tenantId, PlanTier newPlan, LeafContext context);
    Task<Result<Unit>> IncrementUsageAsync(string tenantId, LeafContext context);
    Task<Result<Tenant>> GetByStripeCustomerIdAsync(string stripeCustomerId, LeafContext context);
}
