using MonadicLeaf.Modules.Tenants.Application.Commands;
using MonadicLeaf.Modules.Tenants.Application.Queries;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Plan;
using MonadicSharp;

namespace MonadicLeaf.Modules.Tenants;

public sealed class TenantsService : ITenantsService
{
    private readonly GetTenantQuery _getQuery;
    private readonly CreateTenantCommand _createCmd;
    private readonly UpdateTenantPlanCommand _updatePlanCmd;
    private readonly IncrementUsageCommand _incrementCmd;
    private readonly TenantRepository _repo;

    public TenantsService(
        GetTenantQuery getQuery,
        CreateTenantCommand createCmd,
        UpdateTenantPlanCommand updatePlanCmd,
        IncrementUsageCommand incrementCmd,
        TenantRepository repo)
    {
        _getQuery = getQuery;
        _createCmd = createCmd;
        _updatePlanCmd = updatePlanCmd;
        _incrementCmd = incrementCmd;
        _repo = repo;
    }

    public Task<Result<Tenant>> GetByIdAsync(string tenantId, LeafContext context) =>
        _getQuery.ExecuteAsync(tenantId, context);

    public Task<Result<Tenant>> CreateAsync(string name, LeafContext context) =>
        _createCmd.ExecuteAsync(name, context);

    public Task<Result<Tenant>> UpdatePlanAsync(string tenantId, PlanTier newPlan, LeafContext context) =>
        _updatePlanCmd.ExecuteAsync(tenantId, newPlan, context);

    public Task<Result<Unit>> IncrementUsageAsync(string tenantId, LeafContext context) =>
        _incrementCmd.ExecuteAsync(tenantId, context);

    public Task<Result<Tenant>> GetByStripeCustomerIdAsync(string stripeCustomerId, LeafContext context) =>
        _repo.GetByStripeCustomerIdAsync(stripeCustomerId, context);
}
