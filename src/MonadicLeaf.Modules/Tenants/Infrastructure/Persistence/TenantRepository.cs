using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.SharedKernel.Context;
using MonadicLeaf.SharedKernel.Errors;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;

public sealed class TenantRepository
{
    private readonly TenantDbContext _db;

    public TenantRepository(TenantDbContext db) => _db = db;

    // Green-code: Try.ExecuteAsync at every I/O boundary — no try/catch inside Bind
    public Task<Result<Tenant>> GetByIdAsync(string id, LeafContext context) =>
        Try.ExecuteAsync(() => _db.Tenants.SingleOrDefaultAsync(t => t.Id == id, context.CancellationToken))
           .Bind(tenant => Task.FromResult(
               tenant is not null
                   ? Result<Tenant>.Success(tenant)
                   : Result<Tenant>.Failure(LeafError.TenantNotFound(id))));

    public Task<Result<Tenant>> GetByStripeCustomerIdAsync(string customerId, LeafContext context) =>
        Try.ExecuteAsync(() => _db.Tenants.SingleOrDefaultAsync(t => t.StripeCustomerId == customerId, context.CancellationToken))
           .Bind(tenant => Task.FromResult(
               tenant is not null
                   ? Result<Tenant>.Success(tenant)
                   : Result<Tenant>.Failure(LeafError.TenantNotFound($"stripe:{customerId}"))));

    public Task<Result<Tenant>> AddAsync(Tenant tenant, LeafContext context) =>
        Try.ExecuteAsync(async () =>
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(context.CancellationToken);
            return tenant;
        });

    public Task<Result<Tenant>> UpdateAsync(Tenant tenant, LeafContext context) =>
        Try.ExecuteAsync(async () =>
        {
            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync(context.CancellationToken);
            return tenant;
        });
}
