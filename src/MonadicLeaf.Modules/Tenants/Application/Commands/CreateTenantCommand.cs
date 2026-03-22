using MonadicLeaf.Modules.Tenants.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Tenants.Application.Commands;

public sealed class CreateTenantCommand
{
    private readonly TenantRepository _repo;

    public CreateTenantCommand(TenantRepository repo) => _repo = repo;

    public Task<Result<Tenant>> ExecuteAsync(string name, LeafContext context) =>
        // Green-code: cheapest Bind first — validate before I/O
        ValidateName(name)
            .BindAsync(validName =>
                _repo.AddAsync(Tenant.Create(context.TenantId, validName), context));

    private static Result<string> ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result<string>.Failure(Error.Validation("Tenant name cannot be empty", "name"))
            : name.Length > 256
                ? Result<string>.Failure(Error.Validation("Tenant name too long (max 256 chars)", "name"))
                : Result<string>.Success(name.Trim());
}
