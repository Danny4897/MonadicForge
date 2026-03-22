using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Errors;
using MonadicLeaf.SharedKernel.Plan;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Tenants.Application.Commands;

public sealed class IncrementUsageCommand
{
    private readonly TenantRepository _repo;

    public IncrementUsageCommand(TenantRepository repo) => _repo = repo;

    public Task<Result<Unit>> ExecuteAsync(string tenantId, LeafContext context) =>
        _repo.GetByIdAsync(tenantId, context)
             // Green-code: cheapest check (domain logic) before writing to DB
             .Bind(tenant => Task.FromResult(
                 tenant.HasExceededMonthlyLimit()
                     ? Result<Tenant>.Failure(
                         LeafError.PlanLimitExceeded("analyses",
                             PlanLimits.Plans[tenant.Plan].AnalysesPerMonth))
                     : Result<Tenant>.Success(tenant)))
             // Map only for infallible — IncrementUsage is a simple counter operation
             .Map(tenant => { tenant.IncrementUsage(); return tenant; })
             .Bind(tenant => _repo.UpdateAsync(tenant, context))
             .Map(_ => Unit.Value);
}
