using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Auth.Application.Services;
using MonadicLeaf.Modules.Auth.Contracts;
using MonadicLeaf.Modules.Auth.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Auth.Application.Commands;

public sealed class RegisterCommand
{
    private readonly TenantDbContext _db;
    private readonly ITenantsService _tenants;
    private readonly JwtIssuer _jwt;

    public RegisterCommand(TenantDbContext db, ITenantsService tenants, JwtIssuer jwt)
    {
        _db = db;
        _tenants = tenants;
        _jwt = jwt;
    }

    public Task<Result<AuthResponse>> ExecuteAsync(string email, string password) =>
        // 1. Validate inputs — cheapest, no I/O
        ValidateInputs(email, password)

        // 2. Check email uniqueness — DB read
        .BindAsync(inputs => CheckEmailUnique(inputs.email, inputs.password))

        // 3. Create tenant and user atomically
        .BindAsync(inputs => CreateUserAndTenant(inputs.email, inputs.password));

    // ─── Steps ────────────────────────────────────────────────────────────────

    private static Result<(string email, string password)> ValidateInputs(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<(string, string)>.Failure(Error.Validation("Invalid email address", "email"));
        if (password.Length < 8)
            return Result<(string, string)>.Failure(
                Error.Validation("Password must be at least 8 characters", "password"));
        return Result<(string, string)>.Success((email.ToLowerInvariant().Trim(), password));
    }

    private async Task<Result<(string email, string password)>> CheckEmailUnique(string email, string password)
    {
        // MonadicSharp.DbSetExtensions.AnyAsync already returns Task<Result<bool>>
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists.IsFailure) return Result<(string, string)>.Failure(exists.Error);
        if (exists.Value)
            return Result<(string, string)>.Failure(
                Error.Conflict($"An account with email '{email}' already exists"));
        return Result<(string, string)>.Success((email, password));
    }

    private async Task<Result<AuthResponse>> CreateUserAndTenant(string email, string password)
    {
        var tenantId = Guid.NewGuid().ToString();
        // Temporary context: userId = tenantId (overwritten once user is created)
        var ctx = new LeafContext(tenantId, tenantId, SharedKernel.Plan.PlanTier.Free);

        var tenantResult = await _tenants.CreateAsync(email.Split('@')[0], ctx);
        if (tenantResult.IsFailure) return Result<AuthResponse>.Failure(tenantResult.Error);

        var user = User.Create(email, PasswordHasher.Hash(password), tenantId);
        return await Try.ExecuteAsync(async () =>
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return new AuthResponse(_jwt.Issue(user), email, tenantId, "Free", 0, 50);
        });
    }
}
