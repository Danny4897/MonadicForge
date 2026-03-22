using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;

namespace MonadicLeaf.Modules.Tenants.Application.Queries;

public sealed class GetTenantQuery
{
    private readonly TenantRepository _repo;

    public GetTenantQuery(TenantRepository repo) => _repo = repo;

    public Task<Result<Tenant>> ExecuteAsync(string tenantId, LeafContext context) =>
        _repo.GetByIdAsync(tenantId, context);
}
