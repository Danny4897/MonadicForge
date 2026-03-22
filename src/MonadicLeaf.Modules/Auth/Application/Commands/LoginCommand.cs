using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Auth.Application.Services;
using MonadicLeaf.Modules.Auth.Contracts;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Plan;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicLeaf.Modules.Auth.Application.Commands;

public sealed class LoginCommand
{
    private readonly TenantDbContext _db;
    private readonly JwtIssuer _jwt;

    public LoginCommand(TenantDbContext db, JwtIssuer jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public Task<Result<AuthResponse>> ExecuteAsync(string email, string password) =>
        // 1. Validate inputs — cheapest, no I/O
        ValidateInputs(email, password)

        // 2. Load user — DB read
        .BindAsync(inputs => FindUser(inputs.email, inputs.password))

        // 3. Verify password — CPU
        .Bind(t => VerifyPassword(t.user, t.tenant, t.password).AsTask());

    // ─── Steps ────────────────────────────────────────────────────────────────

    private static Result<(string email, string password)> ValidateInputs(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<(string, string)>.Failure(Error.Validation("Email required", "email"));
        if (string.IsNullOrWhiteSpace(password))
            return Result<(string, string)>.Failure(Error.Validation("Password required", "password"));
        return Result<(string, string)>.Success((email.ToLowerInvariant().Trim(), password));
    }

    private async Task<Result<(Auth.Domain.Entities.User user, Tenants.Domain.Entities.Tenant tenant, string password)>>
        FindUser(string email, string password)
    {
        var userResult = await Try.ExecuteAsync(() =>
            _db.Users.SingleOrDefaultAsync(u => u.Email == email));
        if (userResult.IsFailure) return Result<(Auth.Domain.Entities.User, Tenants.Domain.Entities.Tenant, string)>.Failure(userResult.Error);
        if (userResult.Value is null)
            return Result<(Auth.Domain.Entities.User, Tenants.Domain.Entities.Tenant, string)>.Failure(
                Error.Create("Invalid email or password", "LEAF_AUTH_INVALID"));

        var tenantResult = await Try.ExecuteAsync(() =>
            _db.Tenants.SingleOrDefaultAsync(t => t.Id == userResult.Value.TenantId));
        if (tenantResult.IsFailure) return Result<(Auth.Domain.Entities.User, Tenants.Domain.Entities.Tenant, string)>.Failure(tenantResult.Error);
        if (tenantResult.Value is null)
            return Result<(Auth.Domain.Entities.User, Tenants.Domain.Entities.Tenant, string)>.Failure(
                Error.Create("Tenant not found", "LEAF_TENANT_NOT_FOUND"));

        return Result<(Auth.Domain.Entities.User, Tenants.Domain.Entities.Tenant, string)>
            .Success((userResult.Value, tenantResult.Value, password));
    }

    private Result<AuthResponse> VerifyPassword(
        Auth.Domain.Entities.User user,
        Tenants.Domain.Entities.Tenant tenant,
        string password)
    {
        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return Result<AuthResponse>.Failure(
                Error.Create("Invalid email or password", "LEAF_AUTH_INVALID"));

        var config = PlanLimits.Plans[tenant.Plan];
        return Result<AuthResponse>.Success(new AuthResponse(
            _jwt.Issue(user),
            user.Email,
            user.TenantId,
            tenant.Plan.ToString(),
            tenant.AnalysesUsedThisMonth,
            config.AnalysesPerMonth == int.MaxValue ? -1 : config.AnalysesPerMonth));
    }
}
